// Oracle access shared by generate.js (fake data) and demo.js (demo account activity).
import oracledb from "oracledb";

const batchSize = 1000;

function requiredEnvironment(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

export function connect() {
  return oracledb.getConnection({
    user: requiredEnvironment("ORACLE_USER"),
    password: requiredEnvironment("ORACLE_PASSWORD"),
    connectString: requiredEnvironment("ORACLE_CONNECT_STRING"),
  });
}

export const asObjects = { outFormat: oracledb.OUT_FORMAT_OBJECT };

const string = (maxSize) => ({ type: oracledb.STRING, maxSize });
const number = { type: oracledb.NUMBER };
const id = string(36);
// ISO-8601 with offset, e.g. 2026-11-23T08:20:00.000+00:00
const timestamp = string(35);
const timestampSql = (bind) => `TO_TIMESTAMP_TZ(:${bind}, 'YYYY-MM-DD"T"HH24:MI:SS.FF3TZH:TZM')`;

// Insert order follows the foreign keys. Every column length matches the Liquibase changelogs.
export const tables = [
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

/** Inserts each table's rows from `data[table.key]`, in foreign-key order. Tables missing from `data` are skipped. */
export async function insertAll(connection, data) {
  for (const table of tables) {
    const rows = data[table.key] ?? [];
    for (let start = 0; start < rows.length; start += batchSize) {
      await connection.executeMany(table.sql, rows.slice(start, start + batchSize), { bindDefs: table.bindDefs });
    }
  }
}

/** Runs `work` in one transaction on a new connection: commits if it returns, rolls back if it throws. */
export async function inTransaction(work) {
  const connection = await connect();
  try {
    const result = await work(connection);
    await connection.commit();
    return result;
  } catch (error) {
    try {
      await connection.rollback();
    } catch (rollbackError) {
      throw new AggregateError([error, rollbackError], "The transaction failed and could not be rolled back.");
    }
    throw error;
  } finally {
    await connection.close();
  }
}

export const countBy = (rows, column) =>
  Object.entries(rows.reduce((counts, row) => ({ ...counts, [row[column]]: (counts[row[column]] ?? 0) + 1 }), {}))
    .map(([value, count]) => `${value} ${count}`)
    .join(", ");
