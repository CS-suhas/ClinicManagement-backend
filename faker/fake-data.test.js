import assert from "node:assert/strict";
import test from "node:test";
import { createSeedData, seedClinicName } from "./fake-data.js";

test("creates consistent fake data for the requested patient count", () => {
  const data = createSeedData(100);
  const clinicIds = new Set(data.clinics.map((clinic) => clinic.Id));
  const patientIds = new Set(data.patients.map((patient) => patient.Id));
  const providerIds = new Set(data.providers.map((provider) => provider.Id));
  const allergyIds = new Set(data.allergyTypes.map((allergy) => allergy.Id));

  assert.equal(data.clinics.length, 5);
  assert.equal(new Set(data.clinics.map((clinic) => clinic.Name)).size, 5);
  assert.equal(data.providers.length, 15);
  assert.equal(data.patients.length, 100);
  assert.equal(data.patientAllergies.length, 200);
  assert.equal(data.appointments.length, 150);
  assert.equal(data.clinics[0].Name, seedClinicName);
  assert.equal(new Set(data.patients.map((patient) => patient.Email)).size, 100);
  assert.equal(
    new Set(data.allergyTypes.map((allergy) => allergy.Name)).size,
    data.allergyTypes.length,
  );
  assert.ok(data.patients.every((patient) => patient.Email.endsWith("@example.com")));
  const uuidPattern = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i;
  assert.ok(data.patients.every((patient) => !uuidPattern.test(patient.Email)));
  assert.ok(data.allergyTypes.every((allergy) => !uuidPattern.test(allergy.Name)));
  assert.ok(data.providers.every((provider) => clinicIds.has(provider.ClinicId)));
  assert.ok(
    data.patientAllergies.every(
      (allergy) => patientIds.has(allergy.PatientId) && allergyIds.has(allergy.AllergyId),
    ),
  );
  assert.ok(
    data.appointments.every(
      (appointment) =>
        patientIds.has(appointment.PatientId) && providerIds.has(appointment.ProviderId),
    ),
  );
});

test("rejects an invalid patient count", () => {
  assert.throws(() => createSeedData(0), RangeError);
  assert.throws(() => createSeedData(1.5), RangeError);
});
