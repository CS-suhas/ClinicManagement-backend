import { asObjects, countBy, inTransaction, insertAll } from "./db.js";
import { defaultSeed } from "./fake-data.js";
import { createDemoActivity, demoIdPrefixes } from "./demo-data.js";

const patientEmail = process.env.DEMO_PATIENT_EMAIL ?? "demo-patient@clinic.local";
const doctorEmail = process.env.DEMO_DOCTOR_EMAIL ?? "demo-doctor@clinic.local";
const patientPoolSize = 400;

async function rows(connection, sql, binds = {}) {
  return (await connection.execute(sql, binds, asObjects)).rows;
}

await inTransaction(async (connection) => {
  await connection.execute("DELETE FROM Appointments WHERE Id LIKE :Prefix || '%'", { Prefix: demoIdPrefixes.appointment });
  await connection.execute("DELETE FROM PatientAllergies WHERE Id LIKE :Prefix || '%'", { Prefix: demoIdPrefixes.patientAllergy });

  const [patient] = await rows(
    connection,
    `SELECT p.Id AS "Id", TO_CHAR(p.DateOfBirth, 'YYYY-MM-DD') AS "DateOfBirth"
     FROM Patients p JOIN Users u ON u.Id = p.UserId WHERE LOWER(u.Email) = LOWER(:Email)`,
    { Email: patientEmail },
  );
  const [provider] = await rows(
    connection,
    `SELECT p.Id AS "Id", p.Specialty AS "Specialty", p.ClinicId AS "ClinicId"
     FROM Providers p JOIN Users u ON u.Id = p.UserId WHERE LOWER(u.Email) = LOWER(:Email)`,
    { Email: doctorEmail },
  );
  if (!patient || !provider) {
    throw new Error(
      `Demo accounts not found (patient ${patientEmail}: ${patient ? "ok" : "missing"}, ` +
      `doctor ${doctorEmail}: ${provider ? "ok" : "missing"}). Run demo/demo-accounts.sh first.`,
    );
  }

  const otherProviders = await rows(
    connection,
    `SELECT Id AS "Id", Specialty AS "Specialty" FROM Providers
     WHERE VerificationStatus = 'VERIFIED' AND Id <> :DoctorId
     ORDER BY CASE WHEN ClinicId = :ClinicId THEN 0 ELSE 1 END, Id FETCH FIRST 20 ROWS ONLY`,
    { DoctorId: provider.Id, ClinicId: provider.ClinicId },
  );
  const patients = await rows(
    connection,
    `SELECT Id AS "Id", TO_CHAR(DateOfBirth, 'YYYY-MM-DD') AS "DateOfBirth" FROM Patients
     WHERE Id <> :PatientId ORDER BY Id FETCH FIRST :Limit ROWS ONLY`,
    { PatientId: patient.Id, Limit: patientPoolSize },
  );
  const allergyTypes = await rows(connection, `SELECT Id AS "Id", Name AS "Name" FROM AllergyTypes`);
  const patientAllergyIds = (
    await rows(connection, `SELECT AllergyId AS "AllergyId" FROM PatientAllergies WHERE PatientId = :PatientId`, {
      PatientId: patient.Id,
    })
  ).map((row) => row.AllergyId);
  const booked = await rows(
    connection,
    `SELECT PatientId AS "PatientId", ProviderId AS "ProviderId", AppointmentDateTime AS "AppointmentDateTime"
     FROM Appointments
     WHERE Status IN ('REQUESTED', 'CONFIRMED', 'COMPLETED')
       AND AppointmentDateTime > SYSTIMESTAMP - INTERVAL '70' DAY`,
  );

  const data = createDemoActivity({
    now: new Date(),
    seed: Number(process.env.FAKER_SEED ?? defaultSeed),
    patient,
    provider,
    otherProviders,
    patients,
    allergyTypes,
    patientAllergyIds,
    booked,
  });
  await insertAll(connection, data);

  const mine = (id) => data.appointments.filter((row) => row.PatientId === id || row.ProviderId === id);
  console.log(
    [
      "Demo activity:",
      `  Patient: ${data.patientAllergies.length} allergies, appointments: ${countBy(mine(patient.Id), "Status")}`,
      `  Doctor appointments: ${countBy(mine(provider.Id), "Status")}`,
      ...(patients.length === 0 ? ["  No other patients: run the seeder first to fill the doctor's schedule."] : []),
    ].join("\n"),
  );
});
