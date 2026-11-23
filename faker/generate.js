import oracledb from "oracledb";
import { createSeedData, seedClinicName } from "./fake-data.js";

const targetPatientCount = 100;

function requiredEnvironment(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

function stringBind(maxSize) {
  return { type: oracledb.STRING, maxSize };
}

async function insertMany(connection, sql, rows, bindDefs) {
  if (rows.length === 0) {
    return;
  }

  await connection.executeMany(sql, rows, { bindDefs });
}

async function insertSeedData(connection, patientCount) {
  const {
    clinics,
    providers,
    allergyTypes,
    patients,
    patientAllergies,
    appointments,
  } = createSeedData(patientCount);

  await insertMany(
    connection,
    `INSERT INTO Clinics (Id, Name, Address, PhoneNumber)
     VALUES (:Id, :Name, :Address, :PhoneNumber)`,
    clinics,
    {
      Id: stringBind(36),
      Name: stringBind(200),
      Address: stringBind(500),
      PhoneNumber: stringBind(50),
    },
  );
  await insertMany(
    connection,
    `INSERT INTO Providers (Id, FirstName, LastName, Specialty, ClinicId)
     VALUES (:Id, :FirstName, :LastName, :Specialty, :ClinicId)`,
    providers,
    {
      Id: stringBind(36),
      FirstName: stringBind(100),
      LastName: stringBind(100),
      Specialty: stringBind(200),
      ClinicId: stringBind(36),
    },
  );
  await insertMany(
    connection,
    "INSERT INTO AllergyTypes (Id, Name) VALUES (:Id, :Name)",
    allergyTypes,
    { Id: stringBind(36), Name: stringBind(200) },
  );
  await insertMany(
    connection,
    `INSERT INTO Patients (Id, FirstName, LastName, DateOfBirth, Email, PhoneNumber)
     VALUES (:Id, :FirstName, :LastName, TO_DATE(:DateOfBirth, 'YYYY-MM-DD'), :Email, :PhoneNumber)`,
    patients,
    {
      Id: stringBind(36),
      FirstName: stringBind(100),
      LastName: stringBind(100),
      DateOfBirth: stringBind(10),
      Email: stringBind(320),
      PhoneNumber: stringBind(50),
    },
  );
  await insertMany(
    connection,
    `INSERT INTO PatientAllergies (PatientId, AllergyId, Notes, Severity, Id)
     VALUES (:PatientId, :AllergyId, :Notes, :Severity, :Id)`,
    patientAllergies,
    {
      PatientId: stringBind(36),
      AllergyId: stringBind(36),
      Notes: stringBind(1000),
      Severity: stringBind(20),
      Id: stringBind(36),
    },
  );
  await insertMany(
    connection,
    `INSERT INTO Appointments (Id, PatientId, ProviderId, AppointmentDateTime, ReasonForVisit)
     VALUES (
       :Id,
       :PatientId,
       :ProviderId,
       TO_TIMESTAMP_TZ(:AppointmentDateTime, 'YYYY-MM-DD"T"HH24:MI:SS.FF3TZH:TZM'),
       :ReasonForVisit
     )`,
    appointments,
    {
      Id: stringBind(36),
      PatientId: stringBind(36),
      ProviderId: stringBind(36),
      AppointmentDateTime: stringBind(35),
      ReasonForVisit: stringBind(1000),
    },
  );
}

async function seed() {
  const connection = await oracledb.getConnection({
    user: requiredEnvironment("ORACLE_USER"),
    password: requiredEnvironment("ORACLE_PASSWORD"),
    connectString: requiredEnvironment("ORACLE_CONNECT_STRING"),
  });

  try {
    const seedClinic = await connection.execute(
      "SELECT Id FROM Clinics WHERE Name = :Name",
      { Name: seedClinicName },
    );
    if (seedClinic.rows.length > 0) {
      console.log("Fake data has already been seeded; skipping.");
      return;
    }

    const countResult = await connection.execute("SELECT COUNT(*) FROM Patients");
    const existingPatientCount = Number(countResult.rows[0][0]);
    const patientCount = Math.max(0, targetPatientCount - existingPatientCount);

    if (patientCount > 0) {
      await insertSeedData(connection, patientCount);
    }

    await connection.commit();
    console.log(
      patientCount > 0
        ? `Seeded ${patientCount} patients and related fake data.`
        : "Patient table already meets the seed target; no fake data was added.",
    );
  } catch (error) {
    try {
      await connection.rollback();
    } catch (rollbackError) {
      throw new AggregateError(
        [error, rollbackError],
        "Seeding failed and the transaction could not be rolled back.",
      );
    }
    throw error;
  } finally {
    await connection.close();
  }
}

await seed();
