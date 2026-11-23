// Static catalogues used by the generator. Kept separate from the generation logic so the rules in
// fake-data.js stay readable. Names, places and fees follow the app's Sri Lankan context (fees are in LKR).

export const communities = [
  {
    weight: 74,
    male: [
      "Kasun", "Nuwan", "Chamara", "Tharindu", "Dinesh", "Lahiru", "Supun", "Ruwan", "Sampath", "Asanka",
      "Chathura", "Isuru", "Pradeep", "Nimal", "Sunil", "Kamal", "Ajith", "Mahesh", "Sanjeewa", "Gayan",
      "Dilan", "Janith", "Hasitha", "Pasindu", "Ravindu", "Kavindu", "Sachin", "Dulaj", "Malith", "Thilina",
      "Chanaka", "Upul", "Roshan", "Buddhika", "Lasith", "Amila", "Harsha", "Nalin", "Saman", "Priyantha",
    ],
    female: [
      "Nadeesha", "Dilini", "Sachini", "Ishara", "Chamari", "Hiruni", "Tharushi", "Kaveesha", "Nethmi", "Sanduni",
      "Madhavi", "Anusha", "Shanika", "Iresha", "Nilmini", "Dulani", "Piumi", "Hansika", "Gayani", "Ruwani",
      "Kumari", "Malani", "Sewwandi", "Yashodha", "Thilini", "Imesha", "Oshadi", "Senuri", "Amaya", "Chathurika",
      "Dinithi", "Erandi", "Harshani", "Lakmini", "Manjula", "Nayomi", "Pavithra", "Rasika", "Sajeewani", "Udari",
    ],
    last: [
      "Perera", "Fernando", "Silva", "De Silva", "Jayasinghe", "Wickramasinghe", "Bandara", "Gunawardena",
      "Herath", "Ratnayake", "Weerasinghe", "Kumara", "Rathnayake", "Abeysekara", "Karunaratne", "Liyanage",
      "Wijesinghe", "Samarasinghe", "Ekanayake", "Gamage", "Hettiarachchi", "Jayawardena", "Amarasinghe",
      "Peiris", "Mendis", "Cooray", "Rodrigo", "Munasinghe", "Edirisinghe", "Wijeratne", "Pathirana",
      "Ranasinghe", "Siriwardena", "Kodikara", "Gunasekara", "Weerakoon", "Madushanka", "Senaratne",
      "Dharmasena", "Wanigasekara",
    ],
  },
  {
    weight: 16,
    male: [
      "Arun", "Karthik", "Suresh", "Ramesh", "Prakash", "Vijay", "Senthil", "Kumaran", "Thushyanthan",
      "Sivakumar", "Rajan", "Ganesh", "Mathan", "Niroshan", "Pirasanth", "Vithushan",
    ],
    female: [
      "Priya", "Kavitha", "Lakshmi", "Meena", "Shalini", "Nirosha", "Tharshini", "Divya", "Sharmila", "Vasuki",
      "Abirami", "Janani", "Keerthana", "Thivya",
    ],
    last: [
      "Sivanesan", "Rajaratnam", "Navaratnam", "Thevarajah", "Kandiah", "Selvarajah", "Pillai", "Shanmugam",
      "Krishnan", "Balasubramaniam", "Arulampalam", "Yogarajah", "Ponnambalam", "Sritharan",
    ],
  },
  {
    weight: 10,
    male: ["Mohamed", "Ahamed", "Rizwan", "Fazil", "Imran", "Nazeer", "Rishad", "Ifham", "Aashiq", "Hisham"],
    female: ["Fathima", "Ayesha", "Zainab", "Shifana", "Rifka", "Nusrath", "Hafsa", "Aaqila", "Minha", "Sajeetha"],
    last: ["Mohideen", "Ismail", "Hameed", "Cassim", "Marikar", "Rahuman", "Faleel", "Azeez", "Jabir", "Nizam"],
  },
];

export const emailDomains = [
  { weight: 60, value: "example.com" },
  { weight: 25, value: "example.org" },
  { weight: 15, value: "example.net" },
];

export const mobilePrefixes = ["70", "71", "72", "74", "75", "76", "77", "78"];

// minAge/maxAge bound the patients a specialty sees; sex "F" restricts to female patients.
// ageWeight biases patient choice towards the age groups a specialty typically sees.
export const specialties = {
  "General Practice": {
    primaryCare: true, minAge: 0, maxAge: 120, fee: [1500, 2500],
    reasons: [
      "Fever and sore throat", "Persistent cough", "Headache", "Back pain", "Stomach ache and diarrhoea",
      "Medical certificate", "Blood pressure check", "Minor wound dressing", "Fatigue", "Dengue screening",
    ],
  },
  "Family Medicine": {
    primaryCare: true, minAge: 0, maxAge: 120, fee: [1800, 3000],
    reasons: [
      "Annual health check", "Hypertension review", "Diabetes follow-up", "Cholesterol review",
      "Vaccination", "Cold and flu symptoms", "Prescription renewal", "Review of blood test results",
      "Sleep problems", "Weight management",
    ],
  },
  "Internal Medicine": {
    primaryCare: true, minAge: 16, maxAge: 120, fee: [2500, 4500], ageWeight: (age) => 0.5 + age / 50,
    reasons: [
      "Diabetes management", "Hypertension follow-up", "Thyroid review", "Chronic kidney disease review",
      "Unexplained weight loss", "Anaemia work-up", "Review of blood test results", "Recurrent fever",
    ],
  },
  Pediatrics: {
    primaryCare: true, minAge: 0, maxAge: 17, fee: [2000, 3500],
    reasons: [
      "Well-child check", "Childhood vaccination", "Fever", "Ear pain", "Wheezing", "Skin rash",
      "Growth and development review", "Diarrhoea and vomiting", "School medical",
    ],
  },
  Cardiology: {
    minAge: 18, maxAge: 120, fee: [3500, 6500], ageWeight: (age) => Math.max(0.2, (age - 25) / 20),
    reasons: [
      "Chest pain evaluation", "Palpitations", "ECG review", "Post-angioplasty follow-up",
      "Heart failure review", "Echocardiogram results", "Shortness of breath on exertion",
    ],
  },
  Endocrinology: {
    minAge: 12, maxAge: 120, fee: [3500, 6000], ageWeight: (age) => 0.5 + age / 60,
    reasons: [
      "Type 2 diabetes review", "HbA1c results", "Thyroid nodule follow-up", "Insulin adjustment",
      "Polycystic ovary syndrome review", "Osteoporosis assessment",
    ],
  },
  Dermatology: {
    minAge: 0, maxAge: 120, fee: [3000, 5000],
    reasons: [
      "Eczema flare-up", "Acne treatment", "Psoriasis review", "Mole check", "Fungal skin infection",
      "Hair loss", "Urticaria",
    ],
  },
  "Obstetrics and Gynecology": {
    minAge: 16, maxAge: 75, sex: "F", fee: [3500, 6000],
    ageWeight: (age) => (age >= 20 && age <= 40 ? 2 : 0.6),
    reasons: [
      "Antenatal visit", "Dating scan review", "Postnatal check", "Menstrual irregularities",
      "Pap smear", "Menopause symptoms", "Family planning consultation",
    ],
  },
  Orthopedics: {
    minAge: 5, maxAge: 120, fee: [3500, 6000],
    reasons: [
      "Knee pain", "Lower back pain", "Fracture follow-up", "Shoulder pain", "Sports injury",
      "Osteoarthritis review", "Plaster removal",
    ],
  },
  "ENT (Otolaryngology)": {
    minAge: 0, maxAge: 120, fee: [3000, 5000],
    reasons: [
      "Chronic sinusitis", "Ear wax removal", "Hearing loss", "Recurrent tonsillitis", "Nasal blockage",
      "Vertigo",
    ],
  },
  Psychiatry: {
    minAge: 14, maxAge: 120, fee: [4000, 7000],
    reasons: [
      "Anxiety follow-up", "Depression review", "Medication review", "Insomnia", "Stress management",
    ],
  },
  Ophthalmology: {
    minAge: 0, maxAge: 120, fee: [3000, 5500], ageWeight: (age) => 0.6 + age / 70,
    reasons: [
      "Blurred vision", "Diabetic eye screening", "Cataract assessment", "Glaucoma follow-up",
      "Red eye", "Spectacle prescription",
    ],
  },
  Pulmonology: {
    minAge: 12, maxAge: 120, fee: [3500, 6000],
    reasons: [
      "Asthma review", "Chronic cough", "COPD follow-up", "Spirometry results", "Post-pneumonia review",
    ],
  },
  Neurology: {
    minAge: 12, maxAge: 120, fee: [4000, 7000],
    reasons: ["Migraine follow-up", "Seizure review", "Numbness and tingling", "Dizziness", "Stroke follow-up"],
  },
};

export const genericReasons = ["Follow-up visit", "Review of test results", "Prescription renewal", "Second opinion"];

// Fixed clinic roster. Sizes are deliberately uneven so lists, dashboards and summaries look different per
// clinic. The first entry is the seed marker clinic; its Id and Name are checked by generate.js.
export const clinicProfiles = [
  {
    name: null, // replaced by seedClinicName
    address: "No. 12, Ward Place, Colombo 07",
    phone: "+94 11 269 4410",
    providerCount: 3,
    specialtyWeights: { "General Practice": 2, Pediatrics: 1, "Internal Medicine": 1 },
  },
  {
    name: "Colombo City Medical Centre",
    address: "No. 245, Galle Road, Colombo 03",
    phone: "+94 11 257 3300",
    providerCount: 12,
    specialtyWeights: {
      "General Practice": 2, "Internal Medicine": 2, Cardiology: 2, Endocrinology: 1, Dermatology: 1,
      "Obstetrics and Gynecology": 1, Orthopedics: 1, Neurology: 1, Psychiatry: 1, Pediatrics: 1,
    },
  },
  {
    name: "Kandy Lakeside Hospital Outpatients",
    address: "No. 58, Dalada Veediya, Kandy",
    phone: "+94 81 222 4561",
    providerCount: 9,
    specialtyWeights: {
      "Family Medicine": 2, "Internal Medicine": 1, Pediatrics: 1, Cardiology: 1, Ophthalmology: 1,
      "ENT (Otolaryngology)": 1, Pulmonology: 1, "Obstetrics and Gynecology": 1,
    },
  },
  {
    name: "Galle Family Health Clinic",
    address: "No. 17, Wakwella Road, Galle",
    phone: "+94 91 223 8890",
    providerCount: 7,
    specialtyWeights: { "Family Medicine": 3, "General Practice": 2, Pediatrics: 1, "Obstetrics and Gynecology": 1 },
  },
  {
    name: "Negombo Children's Clinic",
    address: "No. 102, Main Street, Negombo",
    phone: "+94 31 223 5577",
    providerCount: 6,
    specialtyWeights: { Pediatrics: 4, "ENT (Otolaryngology)": 1, Dermatology: 1 },
  },
  {
    name: "Jaffna Community Medical Centre",
    address: "No. 33, Hospital Road, Jaffna",
    phone: null, // exercises the optional clinic phone number
    providerCount: 4,
    specialtyWeights: { "General Practice": 2, "Family Medicine": 1, "Internal Medicine": 1 },
  },
  {
    name: "Kurunegala Skin & Allergy Clinic",
    address: "No. 9, Colombo Road, Kurunegala",
    phone: "+94 37 222 9014",
    providerCount: 3,
    specialtyWeights: { Dermatology: 2, "ENT (Otolaryngology)": 1, Pulmonology: 1 },
  },
  {
    name: "Matara Heart & Diabetes Centre",
    address: "No. 71, Anagarika Dharmapala Mawatha, Matara",
    phone: "+94 41 222 6735",
    providerCount: 2,
    specialtyWeights: { Cardiology: 1, Endocrinology: 1 },
  },
];

// Providers outside any clinic, by verification state. Only verified providers may file join requests.
export const unassignedProviderPlan = { PENDING: 4, REJECTED: 2, VERIFIED: 6 };

// prevalence: relative chance a given allergic patient has it. severityWeights: [MILD, MODERATE, SEVERE].
export const allergyCatalog = [
  { name: "Penicillin", category: "drug", prevalence: 12, severityWeights: [40, 40, 20] },
  { name: "Sulfonamides", category: "drug", prevalence: 5, severityWeights: [50, 35, 15] },
  { name: "Aspirin", category: "drug", prevalence: 4, severityWeights: [45, 40, 15] },
  { name: "Ibuprofen and other NSAIDs", category: "drug", prevalence: 5, severityWeights: [50, 35, 15] },
  { name: "Codeine", category: "drug", prevalence: 2, severityWeights: [55, 35, 10] },
  { name: "Iodinated contrast media", category: "drug", prevalence: 2, severityWeights: [40, 40, 20] },
  { name: "Latex", category: "contact", prevalence: 3, severityWeights: [50, 35, 15] },
  { name: "Nickel", category: "contact", prevalence: 4, severityWeights: [75, 22, 3] },
  { name: "Peanuts", category: "food", prevalence: 6, severityWeights: [25, 35, 40] },
  { name: "Tree nuts", category: "food", prevalence: 3, severityWeights: [30, 35, 35] },
  { name: "Shellfish", category: "food", prevalence: 7, severityWeights: [30, 40, 30] },
  { name: "Fish", category: "food", prevalence: 3, severityWeights: [40, 35, 25] },
  { name: "Eggs", category: "food", prevalence: 3, severityWeights: [55, 35, 10], childBias: 3 },
  { name: "Cow's milk", category: "food", prevalence: 3, severityWeights: [60, 30, 10], childBias: 3 },
  { name: "Soy", category: "food", prevalence: 1, severityWeights: [65, 30, 5] },
  { name: "Wheat", category: "food", prevalence: 1, severityWeights: [60, 30, 10] },
  { name: "Sesame", category: "food", prevalence: 1, severityWeights: [40, 35, 25] },
  { name: "House dust mites", category: "environmental", prevalence: 14, severityWeights: [65, 30, 5] },
  { name: "Grass pollen", category: "environmental", prevalence: 7, severityWeights: [70, 27, 3] },
  { name: "Mould spores", category: "environmental", prevalence: 4, severityWeights: [70, 27, 3] },
  { name: "Cat dander", category: "environmental", prevalence: 4, severityWeights: [65, 30, 5] },
  { name: "Bee and wasp stings", category: "insect", prevalence: 3, severityWeights: [35, 35, 30] },
];

export const allergyNotes = {
  drug: {
    MILD: ["Mild rash after second dose.", "Itching only; resolved without treatment."],
    MODERATE: ["Widespread hives; treated with antihistamines.", "Facial swelling, no breathing difficulty."],
    SEVERE: ["Anaphylaxis documented in hospital record. Avoid entire drug class.", "Stevens-Johnson reaction reported. Never re-challenge."],
  },
  contact: {
    MILD: ["Contact dermatitis on exposure.", "Localised redness and itching."],
    MODERATE: ["Blistering rash on prolonged contact.", "Use non-latex gloves for procedures."],
    SEVERE: ["Generalised urticaria with wheeze on exposure. Latex-free room required."],
  },
  food: {
    MILD: ["Tingling mouth after eating.", "Mild stomach upset."],
    MODERATE: ["Hives and vomiting after small amounts.", "Lip swelling; carries antihistamines."],
    SEVERE: ["Anaphylaxis history. Carries adrenaline auto-injector.", "Throat tightness within minutes of exposure. Auto-injector prescribed."],
  },
  environmental: {
    MILD: ["Seasonal sneezing and runny nose.", "Itchy eyes; manages with antihistamines."],
    MODERATE: ["Triggers asthma symptoms; uses inhaler.", "Persistent rhinitis; on nasal steroid spray."],
    SEVERE: ["Severe asthma exacerbations on exposure; previous admission."],
  },
  insect: {
    MILD: ["Large local swelling at sting site."],
    MODERATE: ["Hives beyond the sting site."],
    SEVERE: ["Anaphylaxis after wasp sting. Carries adrenaline auto-injector."],
  },
};
