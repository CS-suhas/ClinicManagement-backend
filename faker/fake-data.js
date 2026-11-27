import { Faker, en } from "@faker-js/faker";
import {
  allergyCatalog,
  allergyNotes,
  clinicProfiles,
  communities,
  emailDomains,
  genericReasons,
  mobilePrefixes,
  specialties,
  selfRegisteredProviderPlan,
} from "./reference-data.js";

export const seedClinicName = "Training Clinic (clinic-management-demo-v2)";
export const seedClinicId = "5eed0002-c11c-4a5e-8000-000000000001";
export const defaultSeed = 20240917;

export const appointmentSettings = Object.freeze({
  durationMinutes: 20,
  clinicStartMinutes: 8 * 60,
  clinicEndMinutes: 16 * 60,
});

export const appointmentStatuses = ["REQUESTED", "CONFIRMED", "DECLINED", "CANCELLED", "COMPLETED"];
export const activeAppointmentStatuses = ["REQUESTED", "CONFIRMED"];
export const inactiveAppointmentStatuses = ["DECLINED", "CANCELLED"];
export const providerVerificationStatuses = ["PENDING", "VERIFIED", "REJECTED"];
export const allergySeverities = ["MILD", "MODERATE", "SEVERE"];
export const genders = ["MALE", "FEMALE", "OTHER"];

const minute = 60_000;
const hour = 60 * minute;
const day = 24 * hour;
const year = 365.25 * day;

const slotMs = appointmentSettings.durationMinutes * minute;
const slotsPerDay =
  (appointmentSettings.clinicEndMinutes - appointmentSettings.clinicStartMinutes) /
  appointmentSettings.durationMinutes;

const patientAgeBands = [
  { weight: 7, value: [0.5, 5] },
  { weight: 8, value: [5, 13] },
  { weight: 5, value: [13, 18] },
  { weight: 30, value: [18, 40] },
  { weight: 32, value: [40, 65] },
  { weight: 18, value: [65, 92] },
];

export function toTimestamp(ms) {
  return ms == null ? null : new Date(ms).toISOString().replace("Z", "+00:00");
}

function toDateString(ms) {
  return new Date(ms).toISOString().slice(0, 10);
}

function utcMidnight(ms) {
  const date = new Date(ms);
  return Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
}

function ageInYears(dobMs, atMs) {
  return (atMs - dobMs) / year;
}

export function createSeedData({
  patientCount,
  now = new Date(),
  seed = defaultSeed,
  existingPatients = [],
  existingAllergyTypes = [],
  existingEmails = [],
  pastWeeks = 10,
  futureWeeks = 5,
} = {}) {
  if (!Number.isInteger(patientCount) || patientCount < 0) {
    throw new RangeError("Patient count must be a non-negative integer.");
  }
  if (patientCount === 0 && existingPatients.length === 0) {
    throw new RangeError("At least one patient is needed: pass a positive patient count or existing patients.");
  }
  if (!Number.isSafeInteger(seed)) {
    throw new TypeError("seed must be an integer.");
  }
  if (!(now instanceof Date) || Number.isNaN(now.getTime())) {
    throw new TypeError("now must be a valid Date.");
  }

  const faker = new Faker({ locale: [en] });
  faker.seed(seed);

  const nowMs = now.getTime();
  const todayMs = utcMidnight(nowMs);
  const between = (min, max) => min + faker.number.float({ min: 0, max: 1 }) * (max - min);
  const chance = (probability) => faker.number.float({ min: 0, max: 1 }) < probability;
  const weighted = (weights) =>
    faker.helpers.weightedArrayElement(Object.entries(weights).map(([value, weight]) => ({ value, weight })));

  const communityChoices = communities.map((community) => ({ weight: community.weight, value: community }));

  function createPerson(sex = faker.helpers.arrayElement(["M", "F"])) {
    const community = faker.helpers.weightedArrayElement(communityChoices);
    return {
      sex,
      firstName: faker.helpers.arrayElement(sex === "M" ? community.male : community.female),
      lastName: faker.helpers.arrayElement(community.last),
    };
  }

  function providerGender(sex) {
    if (chance(0.05)) return null;
    if (chance(0.015)) return "OTHER";
    return sex === "M" ? "MALE" : "FEMALE";
  }

  function providerFee(specialty) {
    if (chance(0.08)) return null;
    const [min, max] = specialties[specialty].fee;
    return Math.round(between(min, max) / 50) * 50;
  }

  const usedProviderNames = new Set();
  function uniqueProviderPerson() {
    for (let attempt = 0; attempt < 200; attempt++) {
      const person = createPerson();
      const key = `${person.firstName} ${person.lastName}`;
      if (!usedProviderNames.has(key)) {
        usedProviderNames.add(key);
        return person;
      }
    }
    throw new Error("Could not generate a unique provider name.");
  }

  const clinics = clinicProfiles.map((profile, index) => ({
    Id: index === 0 ? seedClinicId : faker.string.uuid(),
    Name: index === 0 ? seedClinicName : profile.name,
    Address: profile.address,
    PhoneNumber: profile.phone,
    CreatedAtUtc: toTimestamp(nowMs - between(2 * year, 6 * year)),
    UpdatedAtUtc: null,
  }));

  const providers = [];
  const providerMeta = [];

  function workDays() {
    if (chance(0.7)) {
      return chance(0.2) ? [1, 2, 3, 4, 5, 6] : [1, 2, 3, 4, 5];
    }
    const count = faker.number.int({ min: 2, max: 3 });
    return faker.helpers.arrayElements([1, 2, 3, 4, 5, 6], count).sort();
  }

  function allocateSpecialties(specialtyWeights, count) {
    const entries = Object.entries(specialtyWeights);
    const total = entries.reduce((sum, [, weight]) => sum + weight, 0);
    const shares = entries.map(([name, weight], order) => {
      const exact = (weight * count) / total;
      return { name, order, seats: Math.floor(exact), remainder: exact - Math.floor(exact) };
    });
    let left = count - shares.reduce((sum, share) => sum + share.seats, 0);
    for (const share of [...shares].sort((a, b) => b.remainder - a.remainder || a.order - b.order)) {
      if (left-- <= 0) break;
      share.seats++;
    }
    const roster = shares.flatMap((share) => Array(share.seats).fill(share.name));
    return [
      ...roster.filter((name) => specialties[name].primaryCare),
      ...faker.helpers.shuffle(roster.filter((name) => !specialties[name].primaryCare)),
    ];
  }

  clinicProfiles.forEach((profile, clinicIndex) => {
    const clinic = clinics[clinicIndex];
    const roster = allocateSpecialties(profile.specialtyWeights, profile.providerCount);

    for (const specialty of roster) {
      const person = uniqueProviderPerson();
      const id = faker.string.uuid();

      let createdMs;
      let updatedMs = null;
      let availableFromMs;
      if (chance(0.35)) {
        const verifiedMs = nowMs - between(120 * day, 700 * day);
        createdMs = verifiedMs - between(1 * day, 10 * day);
        updatedMs = verifiedMs;
        availableFromMs = verifiedMs;
      } else {
        createdMs = nowMs - between(1 * year, 5 * year);
        availableFromMs = createdMs;
        if (chance(0.2)) updatedMs = between(createdMs, nowMs - day);
      }

      providers.push({
        Id: id,
        FirstName: person.firstName,
        LastName: person.lastName,
        Specialty: specialty,
        ClinicId: clinic.Id,
        VerificationStatus: "VERIFIED",
        Fee: providerFee(specialty),
        Gender: providerGender(person.sex),
        CreatedAtUtc: toTimestamp(createdMs),
        UpdatedAtUtc: toTimestamp(updatedMs),
      });

      const busyness = specialties[specialty].primaryCare ? 1.2 : 1;
      providerMeta.push({
        id,
        clinicId: clinic.Id,
        specialty,
        rules: specialties[specialty],
        workDays: workDays(),
        load: between(0.04, 0.11) * busyness,
        availableFromMs,
      });
    }
  });

  const allSpecialtyNames = Object.keys(specialties);
  let registeredClinic = 0;
  for (const [status, count] of Object.entries(selfRegisteredProviderPlan)) {
    for (let index = 0; index < count; index++) {
      const person = uniqueProviderPerson();
      const specialty = faker.helpers.arrayElement(allSpecialtyNames);
      const id = faker.string.uuid();
      const homeClinicId = clinics[registeredClinic++ % clinics.length].Id;
      let createdMs;
      let updatedMs = null;

      if (status === "PENDING") {
        createdMs = nowMs - between(1 * day, 20 * day);
      } else if (status === "REJECTED") {
        createdMs = nowMs - between(30 * day, 120 * day);
        updatedMs = createdMs + between(1 * day, 7 * day);
      } else {
        createdMs = nowMs - between(20 * day, 150 * day);
        updatedMs = createdMs + between(1 * day, 7 * day);
      }

      providers.push({
        Id: id,
        FirstName: person.firstName,
        LastName: person.lastName,
        Specialty: specialty,
        ClinicId: homeClinicId,
        VerificationStatus: status,
        Fee: providerFee(specialty),
        Gender: providerGender(person.sex),
        CreatedAtUtc: toTimestamp(createdMs),
        UpdatedAtUtc: toTimestamp(updatedMs),
      });
    }
  }

  const existingAllergyIds = new Map(
    existingAllergyTypes.map((type) => [type.Name.trim().toLowerCase(), type.Id]),
  );
  const allergyTypes = [];
  const allergyIdByName = new Map();
  for (const allergy of allergyCatalog) {
    const existingId = existingAllergyIds.get(allergy.name.toLowerCase());
    if (existingId) {
      allergyIdByName.set(allergy.name, existingId);
      continue;
    }
    const id = faker.string.uuid();
    allergyIdByName.set(allergy.name, id);
    allergyTypes.push({
      Id: id,
      Name: allergy.name,
      CreatedAtUtc: toTimestamp(nowMs - between(2.5 * year, 3 * year)),
      UpdatedAtUtc: null,
    });
  }

  const usedEmails = new Set(existingEmails.map((email) => email.trim().toLowerCase()));
  function uniqueEmail(firstName, lastName) {
    const first = firstName.toLowerCase().replace(/[^a-z]/g, "");
    const last = lastName.toLowerCase().replace(/[^a-z]/g, "");
    const local = faker.helpers.weightedArrayElement([
      { weight: 50, value: `${first}.${last}` },
      { weight: 15, value: `${first}${last}` },
      { weight: 15, value: `${first[0]}.${last}` },
      { weight: 10, value: `${first}_${last}` },
      { weight: 10, value: `${first}.${last}${faker.number.int({ min: 70, max: 99 })}` },
    ]);
    const domain = faker.helpers.weightedArrayElement(emailDomains);
    for (let suffix = 1; suffix < 1000; suffix++) {
      const email = `${local}${suffix === 1 ? "" : suffix}@${domain}`;
      if (!usedEmails.has(email)) {
        usedEmails.add(email);
        return email;
      }
    }
    throw new Error("Could not generate a unique patient email.");
  }

  const patients = [];
  const patientMeta = [];
  for (let index = 0; index < patientCount; index++) {
    const person = createPerson();
    const [minAge, maxAge] = faker.helpers.weightedArrayElement(patientAgeBands);
    const dobMs = todayMs - Math.round(between(minAge, maxAge) * year / day) * day;
    const earliestCreatedMs = Math.max(dobMs + 7 * day, nowMs - 3 * year);
    const createdMs = chance(0.9)
      ? between(earliestCreatedMs, nowMs - 120 * day)
      : between(nowMs - 60 * day, nowMs - day);
    const id = faker.string.uuid();

    patients.push({
      Id: id,
      FirstName: person.firstName,
      LastName: person.lastName,
      DateOfBirth: toDateString(dobMs),
      Email: uniqueEmail(person.firstName, person.lastName),
      PhoneNumber: `+94 ${faker.helpers.arrayElement(mobilePrefixes)} ${faker.string.numeric(3)} ${faker.string.numeric(4)}`,
      CreatedAtUtc: toTimestamp(createdMs),
      UpdatedAtUtc: toTimestamp(chance(0.15) ? between(createdMs, nowMs - hour) : null),
    });
    patientMeta.push({ id, sex: person.sex, dobMs, createdMs, isNew: true });
  }
  for (const existing of existingPatients) {
    patientMeta.push({ id: existing.Id, sex: null, dobMs: Date.parse(`${existing.DateOfBirth}T00:00:00Z`), createdMs: 0, isNew: false });
  }
  for (const patient of patientMeta) {
    const age = ageInYears(patient.dobMs, nowMs);
    patient.propensity = (1 / (0.25 + faker.number.float({ min: 0, max: 1 }))) * (1 + age / 80);
    patient.visitCap = Math.round(3 + patient.propensity * 4);
    patient.visits = 0;
  }

  const patientAllergies = [];
  for (const patient of patientMeta.filter((meta) => meta.isNew)) {
    const count = faker.helpers.weightedArrayElement([
      { weight: 62, value: 0 }, { weight: 24, value: 1 }, { weight: 9, value: 2 },
      { weight: 4, value: 3 }, { weight: 1, value: 4 },
    ]);
    const isChild = ageInYears(patient.dobMs, nowMs) < 12;
    let candidates = allergyCatalog.map((allergy) => ({
      value: allergy,
      weight: allergy.prevalence * (isChild && allergy.childBias ? allergy.childBias : 1),
    }));
    for (let index = 0; index < count; index++) {
      const allergy = faker.helpers.weightedArrayElement(candidates);
      candidates = candidates.filter((candidate) => candidate.value !== allergy);
      const severity = faker.helpers.weightedArrayElement(
        allergySeverities.map((value, rank) => ({ value, weight: allergy.severityWeights[rank] })),
      );
      patientAllergies.push({
        PatientId: patient.id,
        AllergyId: allergyIdByName.get(allergy.name),
        Notes: chance(0.75) ? faker.helpers.arrayElement(allergyNotes[allergy.category][severity]) : null,
        Severity: severity,
        Id: faker.string.uuid(),
        CreatedAtUtc: toTimestamp(patient.createdMs + between(0, 0.3) * (nowMs - patient.createdMs)),
        UpdatedAtUtc: null,
      });
    }
  }

  function isEligible(patient, rules, atMs) {
    const age = ageInYears(patient.dobMs, atMs);
    if (age < rules.minAge || age >= rules.maxAge + 1) return false;
    return !rules.sex || patient.sex === rules.sex;
  }

  const clinicWeights = clinicProfiles.map((profile, index) => ({ weight: profile.providerCount, value: clinics[index].Id }));
  const panels = new Map(providerMeta.map((provider) => [provider.id, []]));
  const clinicPatients = new Map(clinics.map((clinic) => [clinic.Id, []]));
  for (const patient of patientMeta) {
    const homeClinicId = faker.helpers.weightedArrayElement(clinicWeights);
    clinicPatients.get(homeClinicId).push(patient);

    const eligiblePrimary = providerMeta.filter(
      (provider) => provider.rules.primaryCare && isEligible(patient, provider.rules, nowMs),
    );
    const localPrimary = eligiblePrimary.filter((provider) => provider.clinicId === homeClinicId);
    const primary = faker.helpers.arrayElement(localPrimary.length > 0 ? localPrimary : eligiblePrimary);
    panels.get(primary.id).push(patient);

    const referralCount = faker.helpers.weightedArrayElement([
      { weight: 55, value: 0 }, { weight: 35, value: 1 }, { weight: 10, value: 2 },
    ]);
    const eligibleSpecialists = providerMeta.filter(
      (provider) => !provider.rules.primaryCare && isEligible(patient, provider.rules, nowMs),
    );
    for (let index = 0; index < referralCount && eligibleSpecialists.length > 0; index++) {
      const local = eligibleSpecialists.filter((provider) => provider.clinicId === homeClinicId);
      const pool = local.length > 0 && chance(0.7) ? local : eligibleSpecialists;
      const specialist = faker.helpers.arrayElement(pool);
      if (!panels.get(specialist.id).includes(patient)) panels.get(specialist.id).push(patient);
    }
  }

  function weightedPool(patientsInPool, rules) {
    return patientsInPool
      .filter((patient) => isEligible(patient, rules, nowMs))
      .map((patient) => ({
        value: patient,
        weight: patient.propensity * (rules.ageWeight ? rules.ageWeight(ageInYears(patient.dobMs, nowMs)) : 1),
      }));
  }

  for (const provider of providerMeta) {
    provider.pools = [
      { probability: 0.7, choices: weightedPool(panels.get(provider.id), provider.rules) },
      { probability: 0.2, choices: weightedPool(clinicPatients.get(provider.clinicId), provider.rules) },
      { probability: 0.1, choices: weightedPool(patientMeta, provider.rules) },
    ].filter((pool) => pool.choices.length > 0);
  }

  function pickPatientCandidate(provider) {
    if (provider.pools.length === 0) return null;
    let roll = faker.number.float({ min: 0, max: 1 });
    for (const pool of provider.pools) {
      if (roll < pool.probability) return faker.helpers.weightedArrayElement(pool.choices);
      roll -= pool.probability;
    }
    return faker.helpers.weightedArrayElement(provider.pools.at(-1).choices);
  }

  function chooseStatus(startMs, allowInactive) {
    const endMs = startMs + slotMs;
    let weights;
    if (endMs <= nowMs) {
      const daysAgo = (nowMs - startMs) / day;
      weights = {
        COMPLETED: 80,
        CANCELLED: 9,
        DECLINED: 4,
        CONFIRMED: daysAgo < 7 ? 6 : 0.4,
        REQUESTED: daysAgo < 3 ? 2 : 0,
      };
    } else if (startMs <= nowMs) {
      weights = { CONFIRMED: 1 };
    } else {
      const soon = (startMs - nowMs) / day < 7;
      weights = { REQUESTED: soon ? 14 : 28, CONFIRMED: soon ? 68 : 54, CANCELLED: 12, DECLINED: 5 };
    }
    if (!allowInactive) {
      delete weights.CANCELLED;
      delete weights.DECLINED;
    }
    return weighted(Object.fromEntries(Object.entries(weights).filter(([, weight]) => weight > 0)));
  }

  function buildTimeline(startMs, earliestCreatedMs, allowInactive) {
    const latestCreatedMs = Math.min(startMs - 10 * minute, nowMs - 5 * minute);
    if (latestCreatedMs <= earliestCreatedMs + minute) return null;

    let createdMs = startMs - (30 * minute + faker.number.float({ min: 0, max: 1 }) ** 2 * 28 * day);
    if (createdMs > latestCreatedMs) {
      createdMs = latestCreatedMs - between(0, Math.min(14 * day, latestCreatedMs - earliestCreatedMs));
    }
    if (createdMs < earliestCreatedMs) {
      createdMs = between(earliestCreatedMs, latestCreatedMs);
    }

    const status = chooseStatus(startMs, allowInactive);
    const decisionLimitMs = Math.min(startMs, nowMs);
    let changedMs = null;
    if (status === "CONFIRMED" || status === "DECLINED") {
      changedMs = createdMs + between(0.05, 1) * Math.min(36 * hour, decisionLimitMs - createdMs);
    } else if (status === "CANCELLED") {
      changedMs = createdMs + between(0.1, 1) * (decisionLimitMs - createdMs);
    } else if (status === "COMPLETED") {
      changedMs = Math.min(nowMs, startMs + slotMs + between(0, 4 * hour));
    }
    return { status, createdMs: Math.round(createdMs), changedMs: changedMs == null ? null : Math.round(changedMs) };
  }

  const appointments = [];
  const patientDays = new Set();
  const lastReason = new Map();

  function reasonFor(provider, patient) {
    const key = `${patient.id}|${provider.id}`;
    const previous = lastReason.get(key);
    let reason;
    if (previous && chance(0.5)) {
      reason = previous.startsWith("Follow-up: ") ? previous : `Follow-up: ${previous}`;
    } else {
      const roll = faker.number.float({ min: 0, max: 1 });
      if (roll < 0.05) return null;
      reason = roll < 0.8
        ? faker.helpers.arrayElement(provider.rules.reasons)
        : faker.helpers.arrayElement(genericReasons);
    }
    lastReason.set(key, reason);
    return reason;
  }

  function tryBook(provider, startMs, earliestCreatedMs, allowInactive) {
    const dayKey = startMs - (startMs % day);
    for (let attempt = 0; attempt < 12; attempt++) {
      const patient = pickPatientCandidate(provider);
      if (!patient) return null;
      if (patient.visits >= patient.visitCap) continue;
      if (patientDays.has(`${patient.id}|${dayKey}`)) continue;
      if (!isEligible(patient, provider.rules, startMs)) continue;
      const timeline = buildTimeline(startMs, Math.max(earliestCreatedMs, patient.createdMs, provider.availableFromMs), allowInactive);
      if (!timeline) continue;

      patientDays.add(`${patient.id}|${dayKey}`);
      patient.visits++;
      const appointment = {
        Id: faker.string.uuid(),
        PatientId: patient.id,
        ProviderId: provider.id,
        AppointmentDateTime: toTimestamp(startMs),
        ReasonForVisit: reasonFor(provider, patient),
        Status: timeline.status,
        CreatedAtUtc: toTimestamp(timeline.createdMs),
        StatusChangedAtUtc: toTimestamp(timeline.changedMs),
        UpdatedAtUtc: toTimestamp(timeline.changedMs),
      };
      appointments.push(appointment);
      return { appointment, timeline };
    }
    return null;
  }

  const firstDayMs = todayMs - pastWeeks * 7 * day;
  const lastDayMs = todayMs + futureWeeks * 7 * day;
  const slotIndexes = Array.from({ length: slotsPerDay }, (_, index) => index);
  for (const provider of providerMeta) {
    for (let dayMs = firstDayMs; dayMs <= lastDayMs; dayMs += day) {
      if (!provider.workDays.includes(new Date(dayMs).getUTCDay())) continue;

      const daysAhead = (dayMs - todayMs) / day;
      const horizon = daysAhead <= 0 ? 1 : Math.max(0.2, 1 - daysAhead / 40);
      const bookings = Math.round(slotsPerDay * provider.load * horizon * between(0.5, 1.5));
      const chosen = faker.helpers.arrayElements(slotIndexes, Math.min(bookings, slotsPerDay)).sort((a, b) => a - b);

      for (const slot of chosen) {
        const startMs = dayMs + appointmentSettings.clinicStartMinutes * minute + slot * slotMs;
        if (startMs <= provider.availableFromMs + day) continue;

        const booked = tryBook(provider, startMs, 0, true);
        if (booked && inactiveAppointmentStatuses.includes(booked.timeline.status) && chance(0.4)) {
          tryBook(provider, startMs, booked.timeline.changedMs, false);
        }
      }
    }
  }

  return {
    clinics,
    providers,
    allergyTypes,
    patients,
    patientAllergies,
    appointments,
  };
}
