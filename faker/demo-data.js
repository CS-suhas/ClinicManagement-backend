import { Faker, en } from "@faker-js/faker";
import { allergySeverities, appointmentSettings, defaultSeed, toTimestamp } from "./fake-data.js";
import { allergyCatalog, allergyNotes, genericReasons, specialties } from "./reference-data.js";

// Rows written for the demo accounts get these Id prefixes, so demo.js can replace them on every run without
// touching anything made through the UI. Both are valid GUIDs once the 12-digit counter is appended.
export const demoIdPrefixes = Object.freeze({
  appointment: "d3300000-0000-4000-8001-",
  patientAllergy: "d3300000-0000-4000-8002-",
});

const minute = 60_000;
const hour = 60 * minute;
const day = 24 * hour;
const year = 365.25 * day;
const slotMs = appointmentSettings.durationMinutes * minute;
const slotsPerDay =
  (appointmentSettings.clinicEndMinutes - appointmentSettings.clinicStartMinutes) /
  appointmentSettings.durationMinutes;

// The demo patient's history: a window of days from today, who it is with, and how it ended. Covers every
// status so each patient screen has something in it.
const patientPlan = [
  { days: [-65, -55], with: "doctor", status: "COMPLETED" },
  { days: [-45, -35], with: "other", status: "COMPLETED" },
  { days: [-30, -24], with: "doctor", status: "COMPLETED" },
  { days: [-22, -16], with: "other", status: "CANCELLED" },
  { days: [-15, -11], with: "other", status: "DECLINED" },
  { days: [-10, -4], with: "doctor", status: "COMPLETED" },
  { days: [1, 3], with: "doctor", status: "CONFIRMED" },
  { days: [4, 8], with: "other", status: "REQUESTED" },
  { days: [12, 18], with: "other", status: "CONFIRMED" },
];

// The demo doctor's share of slots on a working day, and how many requests are left waiting for a decision.
const doctorLoad = 0.25;
const minTodayBookings = 5;
const minOpenRequests = 5;

const msOf = (value) => (value instanceof Date ? value.getTime() : Date.parse(value));
const dobMsOf = (value) => (value instanceof Date ? value.getTime() : Date.parse(`${value}T00:00:00Z`));

function utcMidnight(ms) {
  const date = new Date(ms);
  return Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
}

/**
 * Builds activity for the demo patient and demo doctor (see demo.js). Pure: random choices come from a
 * Faker instance seeded with `seed`, and dates are relative to `now`.
 *
 * @param {object} options
 * @param {Date} [options.now] Reference time.
 * @param {number} [options.seed] Faker seed.
 * @param {{Id: string, DateOfBirth: string}} options.patient The demo patient.
 * @param {{Id: string, Specialty: string}} options.provider The demo doctor.
 * @param {{Id: string, Specialty: string}[]} [options.otherProviders] Verified providers the demo patient can
 *   also see, closest first (same clinic first).
 * @param {{Id: string, DateOfBirth: string}[]} [options.patients] Patients the demo doctor can see.
 * @param {{Id: string, Name: string}[]} [options.allergyTypes] Allergy types already in the database.
 * @param {string[]} [options.patientAllergyIds] AllergyIds the demo patient already has.
 * @param {{PatientId: string, ProviderId: string, AppointmentDateTime: Date|string}[]} [options.booked]
 *   Requested, confirmed and completed appointments already in the database, which keep their slots.
 */
export function createDemoActivity({
  now = new Date(),
  seed = defaultSeed,
  patient,
  provider,
  otherProviders = [],
  patients = [],
  allergyTypes = [],
  patientAllergyIds = [],
  booked = [],
  pastWeeks = 6,
  futureWeeks = 3,
}) {
  if (!patient?.Id || !provider?.Id) {
    throw new TypeError("The demo patient and demo provider are required.");
  }

  const faker = new Faker({ locale: [en] });
  faker.seed(seed);

  const nowMs = now.getTime();
  const todayMs = utcMidnight(nowMs);
  const between = (min, max) => min + faker.number.float({ min: 0, max: 1 }) * (max - min);
  const chance = (probability) => faker.number.float({ min: 0, max: 1 }) < probability;
  const weighted = (weights) =>
    faker.helpers.weightedArrayElement(
      Object.entries(weights).filter(([, weight]) => weight > 0).map(([value, weight]) => ({ value, weight })),
    );
  const rulesOf = (someone) => specialties[someone.Specialty] ?? specialties["General Practice"];
  const isEligible = (person, rules) => {
    const age = (nowMs - person.dobMs) / year;
    // Sex is not stored for patients, so sex-specific specialties are left out (as the seeder does).
    return age >= rules.minAge && age < rules.maxAge + 1 && !rules.sex;
  };

  const demoPatient = { id: patient.Id, dobMs: dobMsOf(patient.DateOfBirth) };

  // ---------------------------------------------------------------- allergies
  const allergyIdByName = new Map(allergyTypes.map((type) => [type.Name.trim().toLowerCase(), type.Id]));
  const newAllergyTypes = [];
  const patientAllergies = [];
  const alreadyHas = new Set(patientAllergyIds);
  let candidates = allergyCatalog.map((allergy) => ({ value: allergy, weight: allergy.prevalence }));
  while (patientAllergies.length < 3 && candidates.length > 0) {
    const allergy = faker.helpers.weightedArrayElement(candidates);
    candidates = candidates.filter((candidate) => candidate.value !== allergy);

    let allergyId = allergyIdByName.get(allergy.name.toLowerCase());
    if (alreadyHas.has(allergyId)) continue;
    if (!allergyId) {
      allergyId = faker.string.uuid();
      allergyIdByName.set(allergy.name.toLowerCase(), allergyId);
      newAllergyTypes.push({
        Id: allergyId, Name: allergy.name, CreatedAtUtc: toTimestamp(nowMs - 2 * year), UpdatedAtUtc: null,
      });
    }
    const severity = faker.helpers.weightedArrayElement(
      allergySeverities.map((value, rank) => ({ value, weight: allergy.severityWeights[rank] })),
    );
    patientAllergies.push({
      Id: demoIdPrefixes.patientAllergy + String(patientAllergies.length + 1).padStart(12, "0"),
      PatientId: demoPatient.id,
      AllergyId: allergyId,
      Notes: faker.helpers.arrayElement(allergyNotes[allergy.category][severity]),
      Severity: severity,
      CreatedAtUtc: toTimestamp(nowMs - between(90 * day, 400 * day)),
      UpdatedAtUtc: null,
    });
  }

  // ---------------------------------------------------------------- booking
  // A provider holds a slot once; a patient has at most one appointment a day (the seeder's rules).
  const providerSlots = new Set();
  const patientDays = new Set();
  const hold = (providerId, patientId, startMs) => {
    providerSlots.add(`${providerId}|${startMs}`);
    patientDays.add(`${patientId}|${utcMidnight(startMs)}`);
  };
  const isFree = (providerId, patientId, startMs) =>
    !providerSlots.has(`${providerId}|${startMs}`) && !patientDays.has(`${patientId}|${utcMidnight(startMs)}`);
  for (const row of booked) hold(row.ProviderId, row.PatientId, msOf(row.AppointmentDateTime));

  // Creation and status-change times consistent with the domain transitions.
  function timeline(startMs, status) {
    const latestCreatedMs = Math.min(startMs - 30 * minute, nowMs - 5 * minute);
    const createdMs = latestCreatedMs - between(hour, 14 * day);
    const decisionLimitMs = Math.min(startMs, nowMs);
    let changedMs = null;
    if (status === "CONFIRMED" || status === "DECLINED") {
      changedMs = createdMs + between(0.05, 1) * Math.min(36 * hour, decisionLimitMs - createdMs);
    } else if (status === "CANCELLED") {
      changedMs = createdMs + between(0.1, 1) * (decisionLimitMs - createdMs);
    } else if (status === "COMPLETED") {
      changedMs = Math.min(nowMs, startMs + slotMs + between(0, 4 * hour));
    }
    return { createdMs: Math.round(createdMs), changedMs: changedMs == null ? null : Math.round(changedMs) };
  }

  // Same weights as the seeder: past mostly completed, some not closed out yet; future mostly confirmed.
  function statusFor(startMs) {
    if (startMs + slotMs <= nowMs) {
      const daysAgo = (nowMs - startMs) / day;
      return weighted({ COMPLETED: 80, CANCELLED: 9, DECLINED: 4, CONFIRMED: daysAgo < 7 ? 6 : 0.4 });
    }
    if (startMs <= nowMs) return "CONFIRMED"; // in progress
    const soon = (startMs - nowMs) / day < 7;
    return weighted({ REQUESTED: soon ? 14 : 28, CONFIRMED: soon ? 68 : 54, CANCELLED: 12, DECLINED: 5 });
  }

  const lastReason = new Map();
  function reasonFor(someProvider, patientId) {
    const key = `${patientId}|${someProvider.Id}`;
    const previous = lastReason.get(key);
    const reason = previous && chance(0.5)
      ? (previous.startsWith("Follow-up: ") ? previous : `Follow-up: ${previous}`)
      : chance(0.8) ? faker.helpers.arrayElement(rulesOf(someProvider).reasons) : faker.helpers.arrayElement(genericReasons);
    lastReason.set(key, reason);
    return reason;
  }

  const appointments = [];
  function book(someProvider, patientId, startMs, status) {
    const { createdMs, changedMs } = timeline(startMs, status);
    hold(someProvider.Id, patientId, startMs);
    const appointment = {
      Id: demoIdPrefixes.appointment + String(appointments.length + 1).padStart(12, "0"),
      PatientId: patientId,
      ProviderId: someProvider.Id,
      AppointmentDateTime: toTimestamp(startMs),
      ReasonForVisit: reasonFor(someProvider, patientId),
      Status: status,
      CreatedAtUtc: toTimestamp(createdMs),
      StatusChangedAtUtc: toTimestamp(changedMs),
      UpdatedAtUtc: toTimestamp(changedMs),
    };
    appointments.push(appointment);
    return appointment;
  }

  const slotStart = (dayMs, slot) => dayMs + appointmentSettings.clinicStartMinutes * minute + slot * slotMs;
  const isWorkday = (dayMs) => new Date(dayMs).getUTCDay() !== 0; // Monday to Saturday
  const slotIndexes = Array.from({ length: slotsPerDay }, (_, index) => index);

  // ---------------------------------------------------------------- demo patient
  const eligibleOthers = otherProviders.filter((other) => isEligible(demoPatient, rulesOf(other))).slice(0, 4);
  for (const entry of patientPlan) {
    const withProvider = entry.with === "doctor" || eligibleOthers.length === 0
      ? provider
      : faker.helpers.arrayElement(eligibleOthers);
    const [first, last] = entry.days;
    const days = faker.helpers.shuffle(
      Array.from({ length: last - first + 1 }, (_, index) => todayMs + (first + index) * day).filter(isWorkday),
    );
    booking: for (const dayMs of days) {
      for (const slot of faker.helpers.shuffle(slotIndexes)) {
        const startMs = slotStart(dayMs, slot);
        if (isFree(withProvider.Id, demoPatient.id, startMs)) {
          book(withProvider, demoPatient.id, startMs, entry.status);
          break booking;
        }
      }
    }
  }

  // ---------------------------------------------------------------- demo doctor
  const doctorRules = rulesOf(provider);
  const panel = faker.helpers
    .arrayElements(
      patients
        .filter((other) => other.Id !== demoPatient.id)
        .map((other) => ({ id: other.Id, dobMs: dobMsOf(other.DateOfBirth) }))
        .filter((other) => isEligible(other, doctorRules)),
      40,
    )
    .map((member) => ({ value: member, weight: 1 / (0.25 + faker.number.float({ min: 0, max: 1 })) }));

  if (panel.length > 0) {
    for (let dayMs = todayMs - pastWeeks * 7 * day; dayMs <= todayMs + futureWeeks * 7 * day; dayMs += day) {
      if (!isWorkday(dayMs)) continue;
      const daysAhead = (dayMs - todayMs) / day;
      const horizon = daysAhead <= 0 ? 1 : Math.max(0.3, 1 - daysAhead / 30);
      let count = Math.round(slotsPerDay * doctorLoad * horizon * between(0.7, 1.3));
      if (dayMs === todayMs) count = Math.max(count, minTodayBookings);

      const slots = faker.helpers.arrayElements(slotIndexes, Math.min(count, slotsPerDay)).sort((a, b) => a - b);
      for (const slot of slots) {
        const startMs = slotStart(dayMs, slot);
        for (let attempt = 0; attempt < 12; attempt++) {
          const member = faker.helpers.weightedArrayElement(panel);
          if (isFree(provider.Id, member.id, startMs)) {
            book(provider, member.id, startMs, statusFor(startMs));
            break;
          }
        }
      }
    }

    // Leave a queue of requests for the doctor to confirm or decline.
    const upcoming = appointments
      .filter((row) => row.ProviderId === provider.Id && msOf(row.AppointmentDateTime) > nowMs)
      .sort((a, b) => msOf(a.AppointmentDateTime) - msOf(b.AppointmentDateTime));
    let open = upcoming.filter((row) => row.Status === "REQUESTED").length;
    for (const row of upcoming) {
      if (open >= minOpenRequests) break;
      if (row.Status !== "CONFIRMED" || row.PatientId === demoPatient.id) continue;
      Object.assign(row, { Status: "REQUESTED", StatusChangedAtUtc: null, UpdatedAtUtc: null });
      open++;
    }
  }

  return { allergyTypes: newAllergyTypes, patientAllergies, appointments };
}
