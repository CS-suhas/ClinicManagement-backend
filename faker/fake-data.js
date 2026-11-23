import { faker } from "@faker-js/faker";

const specialties = [
  "Family Medicine",
  "Pediatrics",
  "Cardiology",
  "Dermatology",
  "Internal Medicine",
];
const allergyNames = ["Peanuts", "Shellfish", "Pollen"];
const visitReasons = [
  "Annual checkup",
  "Follow-up visit",
  "Seasonal allergies",
  "Consultation",
  "Prescription review",
];

export const seedClinicName = "Training Clinic (clinic-management-demo-v1)";

const clinicCount = 5;
const providersPerClinic = 3;
const maxUniqueAttempts = 100;

function createUniqueValue(generateValue, getKey, usedKeys, description) {
  for (let attempt = 0; attempt < maxUniqueAttempts; attempt++) {
    const value = generateValue();
    const key = getKey(value);
    if (!usedKeys.has(key)) {
      usedKeys.add(key);
      return value;
    }
  }

  throw new Error(`Could not generate a unique ${description}.`);
}

export function createSeedData(patientCount) {
  if (!Number.isInteger(patientCount) || patientCount < 1) {
    throw new RangeError("Patient count must be a positive integer.");
  }

  const clinics = [{
    Id: faker.string.uuid(),
    Name: seedClinicName,
    Address: faker.location.streetAddress(),
    PhoneNumber: faker.phone.number(),
  }];
  const usedClinicNames = new Set([seedClinicName]);
  for (let index = 1; index < clinicCount; index++) {
    const name = createUniqueValue(
      () => `Training ${faker.location.city()} Clinic`,
      (value) => value,
      usedClinicNames,
      "clinic name",
    );
    clinics.push({
      Id: faker.string.uuid(),
      Name: name,
      Address: faker.location.streetAddress(),
      PhoneNumber: faker.phone.number(),
    });
  }

  const providers = clinics.flatMap((clinic) =>
    Array.from({ length: providersPerClinic }, () => ({
      Id: faker.string.uuid(),
      FirstName: faker.person.firstName(),
      LastName: faker.person.lastName(),
      Specialty: faker.helpers.arrayElement(specialties),
      ClinicId: clinic.Id,
    })),
  );
  const usedAllergyNames = new Set();
  const allergyTypes = allergyNames.map((name) => ({
    Id: faker.string.uuid(),
    Name: createUniqueValue(
      () => `Demo ${name}`,
      (value) => value,
      usedAllergyNames,
      "allergy type name",
    ),
  }));
  const usedPatientEmails = new Set();
  const patients = Array.from({ length: patientCount }, () => {
    const patient = createUniqueValue(
      () => {
        const firstName = faker.person.firstName();
        const lastName = faker.person.lastName();
        const emailName = `${firstName}.${lastName}`
          .toLowerCase()
          .replace(/[^a-z0-9._-]/g, "");

        return {
          FirstName: firstName,
          LastName: lastName,
          Email: `${emailName}@example.com`,
        };
      },
      (value) => value.Email,
      usedPatientEmails,
      "patient email",
    );

    return {
      Id: faker.string.uuid(),
      ...patient,
      DateOfBirth: faker.date
        .birthdate({ min: 18, max: 85, mode: "age" })
        .toISOString()
        .slice(0, 10),
      PhoneNumber: faker.phone.number(),
    };
  });
  const patientAllergies = [];
  for (const patient of patients) {
    for (const allergyType of faker.helpers.shuffle(allergyTypes).slice(0, 2)) {
      patientAllergies.push({
        PatientId: patient.Id,
        AllergyId: allergyType.Id,
        Notes: faker.helpers.maybe(() => faker.lorem.sentence(), { probability: 0.6 }),
        Severity: faker.helpers.arrayElement(["MILD", "MODERATE", "SEVERE"]),
        Id: faker.string.uuid(),
      });
    }
  }
  const appointments = Array.from({ length: Math.ceil(patientCount * 1.5) }, () => {
    const date = faker.date.soon({ days: 180, refDate: new Date(Date.now() + 86400000) });
    date.setUTCHours(faker.number.int({ min: 8, max: 15 }), 0, 0, 0);

    return {
      Id: faker.string.uuid(),
      PatientId: faker.helpers.arrayElement(patients).Id,
      ProviderId: faker.helpers.arrayElement(providers).Id,
      AppointmentDateTime: date.toISOString().replace("Z", "+00:00"),
      ReasonForVisit: faker.helpers.arrayElement(visitReasons),
    };
  });

  return {
    clinics,
    providers,
    allergyTypes,
    patients,
    patientAllergies,
    appointments,
  };
}
