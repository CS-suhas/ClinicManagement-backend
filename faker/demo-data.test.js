import assert from "node:assert/strict";
import test from "node:test";
import { appointmentSettings, appointmentStatuses, createSeedData } from "./fake-data.js";
import { createDemoActivity, demoIdPrefixes } from "./demo-data.js";

const now = new Date(process.env.SEED_TEST_NOW ?? "2026-11-23T10:30:00Z");
const nowMs = now.getTime();
const minute = 60_000;
const seeded = createSeedData({ patientCount: 600, now });

const patient = { Id: "d3300000-0000-4000-8000-000000000011", DateOfBirth: "1990-01-15" };
const provider = { Id: "d3300000-0000-4000-8000-000000000012", Specialty: "General Practice" };
const inputs = {
  now,
  patient,
  provider,
  otherProviders: seeded.providers.filter((p) => p.VerificationStatus === "VERIFIED").slice(0, 10),
  patients: seeded.patients,
  allergyTypes: seeded.allergyTypes,
  booked: seeded.appointments.filter((a) => ["REQUESTED", "CONFIRMED", "COMPLETED"].includes(a.Status)),
};
const data = createDemoActivity(inputs);
const ms = (timestamp) => Date.parse(timestamp);
const patientRows = data.appointments.filter((a) => a.PatientId === patient.Id);
const doctorRows = data.appointments.filter((a) => a.ProviderId === provider.Id);

test("gives the demo patient allergies and every appointment status", () => {
  assert.equal(data.patientAllergies.length, 3);
  assert.equal(data.allergyTypes.length, 0, "seeded allergy types are reused");
  assert.equal(new Set(data.patientAllergies.map((a) => a.AllergyId)).size, 3);
  assert.equal(patientRows.length, 9);
  assert.deepEqual(new Set(patientRows.map((a) => a.Status)), new Set(appointmentStatuses));
  assert.ok(patientRows.some((a) => a.ProviderId === provider.Id), "sees the demo doctor");
  assert.ok(patientRows.some((a) => a.ProviderId !== provider.Id), "sees other providers");
});

test("gives the demo doctor history, today and a request queue", () => {
  const today = now.toISOString().slice(0, 10);
  assert.ok(doctorRows.filter((a) => ms(a.AppointmentDateTime) < nowMs).length >= 80);
  const todayCount = doctorRows.filter((a) => a.AppointmentDateTime.startsWith(today)).length;
  assert.ok(now.getUTCDay() === 0 ? todayCount === 0 : todayCount >= 5, `today: ${todayCount}`);
  assert.ok(doctorRows.filter((a) => a.Status === "REQUESTED").length >= 5);
  assert.ok(new Set(doctorRows.map((a) => a.PatientId)).size >= 20);
});

test("uses demo Id prefixes and valid GUIDs", () => {
  const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
  for (const row of data.appointments) assert.ok(row.Id.startsWith(demoIdPrefixes.appointment) && guid.test(row.Id));
  for (const row of data.patientAllergies) assert.ok(row.Id.startsWith(demoIdPrefixes.patientAllergy) && guid.test(row.Id));
  assert.equal(new Set(data.appointments.map((a) => a.Id)).size, data.appointments.length);
});

test("books on the clinic grid without double-booking", () => {
  const slot = appointmentSettings.durationMinutes * minute;
  const providerSlots = new Set(inputs.booked.map((a) => `${a.ProviderId}|${ms(a.AppointmentDateTime)}`));
  const patientDays = new Set(inputs.booked.map((a) => `${a.PatientId}|${a.AppointmentDateTime.slice(0, 10)}`));
  for (const row of data.appointments) {
    const start = new Date(row.AppointmentDateTime);
    const minutes = start.getUTCHours() * 60 + start.getUTCMinutes();
    assert.notEqual(start.getUTCDay(), 0, "no Sundays");
    assert.ok(minutes >= appointmentSettings.clinicStartMinutes && minutes + slot / minute <= appointmentSettings.clinicEndMinutes);
    assert.equal((minutes - appointmentSettings.clinicStartMinutes) % appointmentSettings.durationMinutes, 0);

    const slotKey = `${row.ProviderId}|${start.getTime()}`;
    const dayKey = `${row.PatientId}|${row.AppointmentDateTime.slice(0, 10)}`;
    assert.ok(!providerSlots.has(slotKey), `provider double-booked: ${slotKey}`);
    assert.ok(!patientDays.has(dayKey), `patient booked twice in a day: ${dayKey}`);
    providerSlots.add(slotKey);
    patientDays.add(dayKey);
  }
});

test("keeps statuses and timestamps consistent", () => {
  for (const row of data.appointments) {
    const start = ms(row.AppointmentDateTime);
    const created = ms(row.CreatedAtUtc);
    assert.ok(created < start && created < nowMs, `created after start: ${row.Id}`);
    if (row.Status === "COMPLETED") assert.ok(start + 20 * minute <= nowMs, "completed in the future");
    if (row.Status === "REQUESTED") assert.equal(row.StatusChangedAtUtc, null);
    else {
      assert.ok(ms(row.StatusChangedAtUtc) >= created && ms(row.StatusChangedAtUtc) <= nowMs);
      assert.equal(row.UpdatedAtUtc, row.StatusChangedAtUtc);
    }
  }
});

test("creates missing allergy types and skips ones the patient has", () => {
  const fresh = createDemoActivity({ now, patient, provider, patientAllergyIds: [] });
  assert.equal(fresh.allergyTypes.length, 3);
  assert.equal(fresh.appointments.length, 9, "patient history only, with the doctor");

  const has = data.patientAllergies.map((a) => a.AllergyId);
  const again = createDemoActivity({ ...inputs, patientAllergyIds: has });
  assert.ok(again.patientAllergies.every((a) => !has.includes(a.AllergyId)));
});

test("is deterministic", () => {
  assert.deepEqual(createDemoActivity(inputs), data);
});
