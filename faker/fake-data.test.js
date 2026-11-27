import assert from "node:assert/strict";
import test from "node:test";
import {
  activeAppointmentStatuses,
  allergySeverities,
  appointmentSettings,
  appointmentStatuses,
  createSeedData,
  genders,
  inactiveAppointmentStatuses,
  providerVerificationStatuses,
  seedClinicId,
  seedClinicName,
} from "./fake-data.js";

const now = new Date(process.env.SEED_TEST_NOW ?? "2026-11-23T10:30:00Z");
const seed = process.env.SEED_TEST_SEED === undefined ? undefined : Number(process.env.SEED_TEST_SEED);
const today = now.toISOString().slice(0, 10);
const nowMs = now.getTime();
const minute = 60_000;
const data = createSeedData({ patientCount: 600, now, seed });

const columnLengths = {
  clinics: { Id: 36, Name: 200, Address: 500, PhoneNumber: 50 },
  providers: { Id: 36, FirstName: 100, LastName: 100, Specialty: 200, ClinicId: 36, VerificationStatus: 20, Gender: 10 },
  allergyTypes: { Id: 36, Name: 200 },
  patients: { Id: 36, FirstName: 100, LastName: 100, Email: 320, PhoneNumber: 50 },
  patientAllergies: { Id: 36, PatientId: 36, AllergyId: 36, Notes: 1000, Severity: 20 },
  appointments: { Id: 36, PatientId: 36, ProviderId: 36, ReasonForVisit: 1000, Status: 20 },
};
const requiredColumns = {
  clinics: ["Id", "Name"],
  providers: ["Id", "FirstName", "LastName", "Specialty", "VerificationStatus"],
  allergyTypes: ["Id", "Name"],
  patients: ["Id", "FirstName", "LastName", "DateOfBirth", "Email", "PhoneNumber"],
  patientAllergies: ["Id", "PatientId", "AllergyId", "Severity"],
  appointments: ["Id", "PatientId", "ProviderId", "AppointmentDateTime", "Status"],
};
const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const timestampPattern = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}\+00:00$/;

const ms = (timestamp) => Date.parse(timestamp);
const byId = (rows) => new Map(rows.map((row) => [row.Id, row]));
const countBy = (rows, column) =>
  rows.reduce((counts, row) => counts.set(row[column], (counts.get(row[column]) ?? 0) + 1), new Map());

test("creates the expected volumes", () => {
  assert.equal(data.clinics.length, 8);
  assert.equal(data.patients.length, 600);
  assert.ok(data.providers.length >= 50, `providers: ${data.providers.length}`);
  assert.ok(data.appointments.length >= 3000, `appointments: ${data.appointments.length}`);
  assert.ok(data.allergyTypes.length >= 20);
  assert.ok(data.patientAllergies.length >= 150);

  const clinicSizes = [...countBy(data.providers.filter((p) => p.ClinicId), "ClinicId").values()];
  assert.ok(Math.max(...clinicSizes) >= 3 * Math.min(...clinicSizes), "clinic sizes should vary");
});

test("marks the run with the seed clinic", () => {
  assert.equal(data.clinics[0].Id, seedClinicId);
  assert.equal(data.clinics[0].Name, seedClinicName);
  assert.equal(new Set(data.clinics.map((clinic) => clinic.Name)).size, data.clinics.length);
});

test("respects column lengths, required columns, ids and timestamp formats", () => {
  for (const [table, lengths] of Object.entries(columnLengths)) {
    for (const row of data[table]) {
      for (const [column, maxLength] of Object.entries(lengths)) {
        const value = row[column];
        if (value != null) {
          assert.ok(value.length <= maxLength, `${table}.${column} too long: ${value}`);
        }
      }
      for (const column of requiredColumns[table]) {
        assert.ok(row[column] != null && row[column] !== "", `${table}.${column} is required`);
      }
      assert.match(row.Id, uuidPattern);
      for (const [column, value] of Object.entries(row)) {
        if (column.endsWith("Utc") || column === "AppointmentDateTime") {
          if (value !== null) assert.match(value, timestampPattern, `${table}.${column}`);
        }
      }
      if (row.CreatedAtUtc) assert.ok(ms(row.CreatedAtUtc) <= nowMs, `${table}.CreatedAtUtc in the future`);
    }
    assert.equal(new Set(data[table].map((row) => row.Id)).size, data[table].length, `${table} ids unique`);
  }
});

test("foreign keys resolve and insert order is valid", () => {
  const clinicIds = new Set(data.clinics.map((row) => row.Id));
  const providerIds = new Set(data.providers.map((row) => row.Id));
  const patientIds = new Set(data.patients.map((row) => row.Id));
  const allergyIds = new Set(data.allergyTypes.map((row) => row.Id));

  assert.ok(data.providers.every((row) => clinicIds.has(row.ClinicId)));
  assert.ok(data.patientAllergies.every((row) => patientIds.has(row.PatientId) && allergyIds.has(row.AllergyId)));
  assert.ok(data.appointments.every((row) => patientIds.has(row.PatientId) && providerIds.has(row.ProviderId)));
});

test("unique keys hold", () => {
  const emails = data.patients.map((patient) => patient.Email.toLowerCase());
  assert.equal(new Set(emails).size, emails.length);
  assert.ok(data.patients.every((patient) => /^[a-z0-9._]+@example\.(com|org|net)$/.test(patient.Email)));

  const names = data.allergyTypes.map((type) => type.Name.toLowerCase());
  assert.equal(new Set(names).size, names.length);

  const allergyKeys = data.patientAllergies.map((row) => `${row.PatientId}|${row.AllergyId}`);
  assert.equal(new Set(allergyKeys).size, allergyKeys.length, "PK_PatientAllergies");
});

test("check constraints and enum values are valid, and every value is represented", () => {
  const expectations = [
    [data.providers, "VerificationStatus", providerVerificationStatuses],
    [data.appointments, "Status", appointmentStatuses],
    [data.patientAllergies, "Severity", allergySeverities],
    [data.providers.filter((row) => row.Gender !== null), "Gender", genders.filter((g) => g !== "OTHER")],
  ];
  for (const [rows, column, allowed] of expectations) {
    const counts = countBy(rows, column);
    for (const value of counts.keys()) {
      assert.ok(column === "Gender" ? genders.includes(value) : allowed.includes(value), `${column}: ${value}`);
    }
    for (const value of allowed) assert.ok(counts.get(value) > 0, `${column} ${value} not represented`);
  }

  for (const provider of data.providers) {
    if (provider.Fee !== null) {
      assert.ok(provider.Fee >= 0 && provider.Fee < 100_000_000, "CK_Providers_Fee / NUMBER(10,2)");
      assert.equal(Math.round(provider.Fee * 100) / 100, provider.Fee);
    }
  }
});

test("status proportions are plausible", () => {
  const providers = countBy(data.providers, "VerificationStatus");
  assert.ok(providers.get("VERIFIED") / data.providers.length >= 0.8, "mostly verified providers");

  const past = data.appointments.filter((row) => ms(row.AppointmentDateTime) + 20 * minute <= nowMs);
  const future = data.appointments.filter((row) => ms(row.AppointmentDateTime) > nowMs);
  assert.ok(past.length > 0 && future.length > 0);
  assert.ok(countBy(past, "Status").get("COMPLETED") / past.length >= 0.6, "past mostly completed");
  const futureActive = future.filter((row) => activeAppointmentStatuses.includes(row.Status)).length;
  assert.ok(futureActive / future.length >= 0.7, "future mostly requested or confirmed");

  const severities = countBy(data.patientAllergies, "Severity");
  assert.ok(severities.get("MILD") > severities.get("SEVERE"), "mild allergies outnumber severe");
  const allergic = new Set(data.patientAllergies.map((row) => row.PatientId)).size;
  assert.ok(allergic / data.patients.length > 0.2 && allergic / data.patients.length < 0.6);
});

test("every provider belongs to a clinic", () => {
  for (const provider of data.providers) {
    assert.ok(provider.ClinicId, provider.Id);
  }
});

test("appointments are inside clinic hours on the slot grid", () => {
  const { durationMinutes, clinicStartMinutes, clinicEndMinutes } = appointmentSettings;
  for (const appointment of data.appointments) {
    const start = new Date(appointment.AppointmentDateTime);
    const startMinutes = start.getUTCHours() * 60 + start.getUTCMinutes();
    assert.equal(start.getUTCSeconds(), 0);
    assert.ok(startMinutes >= clinicStartMinutes, appointment.AppointmentDateTime);
    assert.ok(startMinutes + durationMinutes <= clinicEndMinutes, appointment.AppointmentDateTime);
    assert.equal((startMinutes - clinicStartMinutes) % durationMinutes, 0);
    assert.notEqual(start.getUTCDay(), 0, "clinics are closed on Sunday");
  }
});

test("no provider is double-booked and patients are not booked twice a day", () => {
  const slot = appointmentSettings.durationMinutes * minute;
  const occupying = data.appointments.filter((row) => !inactiveAppointmentStatuses.includes(row.Status));
  for (const [providerId, rows] of Map.groupBy(occupying, (row) => row.ProviderId)) {
    const starts = rows.map((row) => ms(row.AppointmentDateTime)).sort((a, b) => a - b);
    for (let index = 1; index < starts.length; index++) {
      assert.ok(starts[index] - starts[index - 1] >= slot, `provider ${providerId} overlaps`);
    }
  }

  const patientDays = data.appointments.map((row) => `${row.PatientId}|${row.AppointmentDateTime.slice(0, 10)}`);
  assert.equal(new Set(patientDays).size, patientDays.length);
});

test("appointments are only with bookable providers and eligible patients", () => {
  const providers = byId(data.providers);
  const patients = byId(data.patients);
  for (const appointment of data.appointments) {
    const provider = providers.get(appointment.ProviderId);
    assert.equal(provider.VerificationStatus, "VERIFIED");
    assert.ok(provider.ClinicId, "appointment lists inner-join the provider's clinic");

    const patient = patients.get(appointment.PatientId);
    const ageYears = (ms(appointment.AppointmentDateTime) - Date.parse(patient.DateOfBirth)) / (365.25 * 86_400_000);
    assert.ok(ageYears > 0);
    if (provider.Specialty === "Pediatrics") assert.ok(ageYears < 18, "pediatric patients are children");
    assert.ok(ms(appointment.CreatedAtUtc) >= ms(patient.CreatedAtUtc), "booked after the patient registered");
    assert.ok(ms(appointment.CreatedAtUtc) >= ms(provider.CreatedAtUtc));
  }
});

test("appointment statuses and timestamps respect the transitions", () => {
  for (const appointment of data.appointments) {
    const start = ms(appointment.AppointmentDateTime);
    const created = ms(appointment.CreatedAtUtc);
    const changed = appointment.StatusChangedAtUtc === null ? null : ms(appointment.StatusChangedAtUtc);
    assert.ok(created < start, "booked before it starts");
    assert.equal(appointment.UpdatedAtUtc, appointment.StatusChangedAtUtc);

    if (appointment.Status === "REQUESTED") {
      assert.equal(changed, null, "a requested appointment has never changed status");
      continue;
    }
    assert.ok(changed !== null && changed >= created && changed <= nowMs, appointment.Id);
    if (appointment.Status === "COMPLETED") {
      assert.ok(start + 20 * minute <= nowMs, "completed appointments are in the past");
      assert.ok(changed >= start + 20 * minute);
    } else {
      assert.ok(changed <= start, `${appointment.Status} is decided before the visit`);
    }
  }
  assert.ok(data.appointments.some((row) => ms(row.AppointmentDateTime) < nowMs));
  assert.ok(data.appointments.some((row) => row.AppointmentDateTime.startsWith(today)) || now.getUTCDay() === 0, "today has appointments");
});

test("patients have plausible birthdates", () => {
  for (const patient of data.patients) {
    const dob = Date.parse(patient.DateOfBirth);
    assert.ok(dob < nowMs, "date of birth is in the past");
    const age = (nowMs - dob) / (365.25 * 86_400_000);
    assert.ok(age >= 0.4 && age <= 93, `age ${age}`);
  }
  const children = data.patients.filter((patient) => nowMs - Date.parse(patient.DateOfBirth) < 18 * 365.25 * 86_400_000);
  assert.ok(children.length > 50 && children.length < 250, `children: ${children.length}`);
});

test("is deterministic for a seed and reference time", () => {
  assert.deepEqual(createSeedData({ patientCount: 600, now, seed }), data);
  const other = createSeedData({ patientCount: 600, now, seed: (seed ?? 0) + 7 });
  assert.notDeepEqual(other.patients, data.patients);
});

test("tops up around existing rows without breaking unique keys", () => {
  const existingPatients = data.patients.slice(0, 50).map((patient) => ({
    Id: patient.Id,
    DateOfBirth: patient.DateOfBirth,
  }));
  const existingEmails = data.patients.map((patient) => patient.Email.toUpperCase());
  const existingAllergyTypes = [{ Id: "00000000-0000-4000-8000-000000000001", Name: "PENICILLIN" }];

  const topUp = createSeedData({ patientCount: 25, now, seed: 99, existingPatients, existingEmails, existingAllergyTypes });
  assert.equal(topUp.patients.length, 25);
  const taken = new Set(existingEmails.map((email) => email.toLowerCase()));
  assert.ok(topUp.patients.every((patient) => !taken.has(patient.Email.toLowerCase())));
  assert.ok(!topUp.allergyTypes.some((type) => type.Name.toLowerCase() === "penicillin"));

  const knownPatients = new Set([...existingPatients, ...topUp.patients].map((patient) => patient.Id));
  assert.ok(topUp.appointments.every((row) => knownPatients.has(row.PatientId)));
  const knownAllergies = new Set([...existingAllergyTypes, ...topUp.allergyTypes].map((type) => type.Id));
  assert.ok(topUp.patientAllergies.every((row) => knownAllergies.has(row.AllergyId)));

  const existingOnly = createSeedData({ patientCount: 0, now, existingPatients });
  assert.equal(existingOnly.patients.length, 0);
  assert.ok(existingOnly.appointments.length > 0);
  assert.ok(existingOnly.appointments.every((row) => knownPatients.has(row.PatientId)));
});

test("rejects invalid options", () => {
  assert.throws(() => createSeedData({ patientCount: -1 }), RangeError);
  assert.throws(() => createSeedData({ patientCount: 1.5 }), RangeError);
  assert.throws(() => createSeedData({ patientCount: 0 }), RangeError);
  assert.throws(() => createSeedData({ patientCount: 10, seed: Number.NaN }), TypeError);
  assert.throws(() => createSeedData({ patientCount: 10, now: new Date("invalid") }), TypeError);
});
