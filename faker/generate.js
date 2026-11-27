import { asObjects, countBy, inTransaction, insertAll, tables } from "./db.js";
import { createSeedData, defaultSeed, seedClinicId, seedClinicName } from "./fake-data.js";

const targetPatientCount = 600;
const existingPatientLimit = 2000;

async function readExistingData(connection) {
  const patientCount = await connection.execute("SELECT COUNT(*) AS Total FROM Patients", {}, asObjects);
  const patients = await connection.execute(
    `SELECT Id AS "Id", TO_CHAR(DateOfBirth, 'YYYY-MM-DD') AS "DateOfBirth"
     FROM Patients ORDER BY Id FETCH FIRST :Limit ROWS ONLY`,
    { Limit: existingPatientLimit },
    asObjects,
  );
  const emails = await connection.execute("SELECT Email FROM Patients");
  const allergyTypes = await connection.execute(
    `SELECT Id AS "Id", Name AS "Name" FROM AllergyTypes ORDER BY Id`,
    {},
    asObjects,
  );

  return {
    patientCount: Number(patientCount.rows[0].TOTAL),
    patients: patients.rows,
    emails: emails.rows.map((row) => row[0]),
    allergyTypes: allergyTypes.rows,
  };
}

function summarize(data) {
  return [
    ...tables.map((table) => `  ${table.name}: ${data[table.key].length}`),
    `  Provider verification: ${countBy(data.providers, "VerificationStatus")}`,
    `  Appointments: ${countBy(data.appointments, "Status")}`,
  ].join("\n");
}

await inTransaction(async (connection) => {
  const marker = await connection.execute(
    "SELECT COUNT(*) FROM Clinics WHERE Name = :Name OR Id = :Id",
    { Name: seedClinicName, Id: seedClinicId },
  );
  if (Number(marker.rows[0][0]) > 0) {
    console.log("Fake data has already been seeded; skipping.");
    return;
  }

  const existing = await readExistingData(connection);
  const patientCount = Math.max(0, targetPatientCount - existing.patientCount);
  const data = createSeedData({
    patientCount,
    now: new Date(),
    seed: Number(process.env.FAKER_SEED ?? defaultSeed),
    existingPatients: existing.patients,
    existingAllergyTypes: existing.allergyTypes,
    existingEmails: existing.emails,
  });

  await insertAll(connection, data);
  console.log(
    `Seeded fake data (${patientCount} new patients, ${existing.patientCount} existing):\n${summarize(data)}`,
  );
});
