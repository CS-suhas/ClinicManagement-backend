import oracledb from "oracledb";
import { createSeedData, defaultSeed, seedClinicId, seedClinicName } from "./fake-data.js";

// The Patients table is topped up to this many rows; existing patients are reused for appointments.
const targetPatientCount = 600;
const batchSize = 1000;
// Upper bound on existing patients pulled in as appointment candidates.
const existingPatientLimit = 2000;

function requiredEnvironment(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

const string = (maxSize) => ({ type: oracledb.STRING, maxSize });
const number = { type: oracledb.NUMBER };
const id = string(36);
// ISO-8601 with offset, e.g. 2026-11-23T08:20:00.000+00:00
const timestamp = string(35);
const timestampSql = (bind) => `TO_TIMESTAMP_TZ(:${bind}, 'YYYY-MM-DD"T"HH24:MI:SS.FF3TZH:TZM')`;

// Insert order follows the foreign keys. Every column length matches the Liquibase changelogs.
const tables = [
  {
    name: "Clinics",
    key: "clinics",
    sql: `INSERT INTO Clinics (Id, Name, Address, PhoneNumber, CreatedAtUtc, UpdatedAtUtc)
          VALUES (:Id, :Name, :Address, :PhoneNumber, ${timestampSql("CreatedAtUtc")}, ${timestampSql("UpdatedAtUtc")})`,
    bindDefs: {
      Id: id, Name: string(200), Address: string(500), PhoneNumber: string(50),
      CreatedAtUtc: timestamp, UpdatedAtUtc: timestamp,
    },
  },
  {
    name: "Providers",
    key: "providers",
    sql: `INSERT INTO Providers
            (Id, FirstName, LastName, Specialty, ClinicId, VerificationStatus, Fee, Gender, CreatedAtUtc, UpdatedAtUtc)
          VALUES (:Id, :FirstName, :LastName, :Specialty, :ClinicId, :VerificationStatus, :Fee, :Gender,
                  ${timestampSql("CreatedAtUtc")}, ${timestampSql("UpdatedAtUtc")})`,
    bindDefs: {
      Id: id, FirstName: string(100), LastName: string(100), Specialty: string(200), ClinicId: id,
      VerificationStatus: string(20), Fee: number, Gender: string(10),
      CreatedAtUtc: timestamp, UpdatedAtUtc: timestamp,
    },
  },
  {
    name: "ClinicJoinRequests",
    key: "clinicJoinRequests",
    sql: `INSERT INTO ClinicJoinRequests (Id, ProviderId, ClinicId, Status, DecidedAtUtc, CreatedAtUtc, UpdatedAtUtc)
          VALUES (:Id, :ProviderId, :ClinicId, :Status, ${timestampSql("DecidedAtUtc")},
                  ${timestampSql("CreatedAtUtc")}, ${timestampSql("UpdatedAtUtc")})`,
    bindDefs: {
      Id: id, ProviderId: id, ClinicId: id, Status: string(20),
      DecidedAtUtc: timestamp, CreatedAtUtc: timestamp, UpdatedAtUtc: timestamp,
    },
  },
  {
    name: "AllergyTypes",
    key: "allergyTypes",
    sql: `INSERT INTO AllergyTypes (Id, Name, CreatedAtUtc, UpdatedAtUtc)
          VALUES (:Id, :Name, ${timestampSql("CreatedAtUtc")}, ${timestampSql("UpdatedAtUtc")})`,
    bindDefs: { Id: id, Name: string(200), CreatedAtUtc: timestamp, UpdatedAtUtc: timestamp },
  },
  {
    name: "Patients",
    key: "patients",
    sql: `INSERT INTO Patients (Id, FirstName, LastName, DateOfBirth, Email, PhoneNumber, CreatedAtUtc, UpdatedAtUtc)
          VALUES (:Id, :FirstName, :LastName, TO_DATE(:DateOfBirth, 'YYYY-MM-DD'), :Email, :PhoneNumber,
                  ${timestampSql("CreatedAtUtc")}, ${timestampSql("UpdatedAtUtc")})`,
    bindDefs: {
      Id: id, FirstName: string(100), LastName: string(100), DateOfBirth: string(10), Email: string(320),
      PhoneNumber: string(50), CreatedAtUtc: timestamp, UpdatedAtUtc: timestamp,
    },
  },
  {
    name: "PatientAllergies",
    key: "patientAllergies",
    sql: `INSERT INTO PatientAllergies (Id, PatientId, AllergyId, Notes, Severity, CreatedAtUtc, UpdatedAtUtc)
          VALUES (:Id, :PatientId, :AllergyId, :Notes, :Severity,
                  ${timestampSql("CreatedAtUtc")}, ${timestampSql("UpdatedAtUtc")})`,
    bindDefs: {
      Id: id, PatientId: id, AllergyId: id, Notes: string(1000), Severity: string(20),
      CreatedAtUtc: timestamp, UpdatedAtUtc: timestamp,
    },
  },
  {
    name: "Appointments",
    key: "appointments",
    sql: `INSERT INTO Appointments
            (Id, PatientId, ProviderId, AppointmentDateTime, ReasonForVisit, Status,
             CreatedAtUtc, StatusChangedAtUtc, UpdatedAtUtc)
          VALUES (:Id, :PatientId, :ProviderId, ${timestampSql("AppointmentDateTime")}, :ReasonForVisit, :Status,
                  ${timestampSql("CreatedAtUtc")}, ${timestampSql("StatusChangedAtUtc")}, ${timestampSql("UpdatedAtUtc")})`,
    bindDefs: {
      Id: id, PatientId: id, ProviderId: id, AppointmentDateTime: timestamp, ReasonForVisit: string(1000),
      Status: string(20), CreatedAtUtc: timestamp, StatusChangedAtUtc: timestamp, UpdatedAtUtc: timestamp,
    },
  },
];

async function insertMany(connection, sql, rows, bindDefs) {
  for (let start = 0; start < rows.length; start += batchSize) {
    await connection.executeMany(sql, rows.slice(start, start + batchSize), { bindDefs });
  }
}

async function readExistingData(connection) {
  const options = { outFormat: oracledb.OUT_FORMAT_OBJECT };
  const patientCount = await connection.execute("SELECT COUNT(*) AS Total FROM Patients", {}, options);
  const patients = await connection.execute(
    `SELECT Id AS "Id", TO_CHAR(DateOfBirth, 'YYYY-MM-DD') AS "DateOfBirth"
     FROM Patients ORDER BY Id FETCH FIRST :Limit ROWS ONLY`,
    { Limit: existingPatientLimit },
    options,
  );
  const emails = await connection.execute("SELECT Email FROM Patients", {}, { outFormat: oracledb.OUT_FORMAT_ARRAY });
  const allergyTypes = await connection.execute(
    `SELECT Id AS "Id", Name AS "Name" FROM AllergyTypes ORDER BY Id`,
    {},
    options,
  );

  return {
    patientCount: Number(patientCount.rows[0].TOTAL),
    patients: patients.rows,
    emails: emails.rows.map((row) => row[0]),
    allergyTypes: allergyTypes.rows,
  };
}

function summarize(data) {
  const countBy = (rows, column) =>
    Object.entries(
      rows.reduce((counts, row) => ({ ...counts, [row[column]]: (counts[row[column]] ?? 0) + 1 }), {}),
    )
      .map(([value, count]) => `${value} ${count}`)
      .join(", ");

  return [
    ...tables.map((table) => `  ${table.name}: ${data[table.key].length}`),
    `  Provider verification: ${countBy(data.providers, "VerificationStatus")}`,
    `  Join requests: ${countBy(data.clinicJoinRequests, "Status")}`,
    `  Appointments: ${countBy(data.appointments, "Status")}`,
  ].join("\n");
}

async function seed() {
  const connection = await oracledb.getConnection({
    user: requiredEnvironment("ORACLE_USER"),
    password: requiredEnvironment("ORACLE_PASSWORD"),
    connectString: requiredEnvironment("ORACLE_CONNECT_STRING"),
  });

  try {
    // The marker clinic is inserted in the same transaction as everything else, so it exists only after a
    // complete, committed run. Checking the fixed Id as well guards against a renamed marker clinic.
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

    for (const table of tables) {
      await insertMany(connection, table.sql, data[table.key], table.bindDefs);
    }

    await connection.commit();
    console.log(
      `Seeded fake data (${patientCount} new patients, ${existing.patientCount} existing):\n${summarize(data)}`,
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
