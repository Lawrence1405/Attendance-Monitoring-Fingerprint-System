import { useState, useEffect, useRef } from "react";
import {
  Fingerprint, LayoutDashboard, CalendarCheck, Database, Users2, LogOut,
  Bell, Search, CheckCircle2, XCircle, Clock, Users, AlertCircle, Download,
  Eye, Edit2, Trash2, ChevronLeft, ChevronRight, Check, History, Shield,
  MapPin, Phone, Calendar, Printer, BarChart2, RefreshCw, BadgeCheck,
  AlertTriangle, X, Plus, ChevronDown, ChevronUp, FileText, SlidersHorizontal,
  Info, User, ChevronsLeft, ChevronsRight, BookOpen, Briefcase, Menu,
} from "lucide-react";
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer,
  LineChart, Line, CartesianGrid, PieChart, Pie, Cell,
} from "recharts";

// ── Types ──────────────────────────────────────────────────────────────────────
type Page = "login"|"dashboard"|"attendance"|"history"|"search"|"reports"|"management";

interface Client {
  // Identity
  clientId: string;
  docketNumber: string;
  fullName: string;
  middleInitial: string;
  gender: "Male"|"Female";
  clientCategory: "Probationer"|"Parolee"|"Pardonee";
  ccNumber: string;
  court: string;
  doNdo: "DO"|"NDO";
  assignedOfficer: string;
  supervisionStart: string;
  supervisionEnd: string;
  supervisionPhase: string;
  registrationDate: string;
  status: "Active"|"Completed"|"Terminated"|"Inactive";
  caseType: "Drug"|"Non-Drug";
  remarks: string;
  // Structured remarks
  finalReport: string;
  finalReportDate: string;
  terminationDate: string;
  violationReport: string;
  violationDate: string;
  courtOrderDisposition: string;
  courtOrderDateSubmitted: string;
  courtOrderDateReceived: string;
  // Fingerprint
  fingerprintId: string;
  fingerprintEnrolled: boolean;
  fingerprintEnrollmentDate: string;
  fingerprintImage?: string;
  // Personal Profile (PPA Form 12)
  piNumber: string;
  alias: string;
  identifyingMarks: string;
  address: string;
  barangay: string;
  contactNumber: string;
  dateOfBirth: string;
  placeOfBirth: string;
  civilStatus: string;
  spouseName: string;
  numberOfDependents: string;
  educationalAttainment: string;
  occupation: string;
  monthlyIncome: string;
  hobbies: string;
  skills: string;
  religiousAffiliation: string;
  // Case Profile (PPA Form 12)
  psiNumber: string;
  criminalCaseNumber: string;
  chargedWith: string;
  dateCommitted: string;
  convictedOf: string;
  dateConvicted: string;
  sentence: string;
  placeOfReferral: string;
  datePsiSubmitted: string;
  custodyStatus: string;
  dateProbationGranted: string;
  dateProbationOrderReceived: string;
  periodOfProbation: string;
  dateFrSubmitted: string;
  dateOfToro: string;
  dateReceivedCase: string;
  investigatingOfficer: string;
  photoInitial: string;
}

interface AttendanceRecord {
  attendanceId: string;
  clientId: string;
  fullName: string;
  caseNumber: string;
  docketNumber: string;
  date: string;
  time: string;
  verifiedBy: string;
  status?: "Present" | "Absent" | "Blank" | string;
}

// ── Constants ──────────────────────────────────────────────────────────────────
const MONTHS = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];
const QUARTERS: Record<string,number[]> = {"Q1":[0,1,2],"Q2":[3,4,5],"Q3":[6,7,8],"Q4":[9,10,11]};
const YEAR = 2026;
const _today = new Date();
const todayStr = _today.toISOString().split("T")[0];
const CUR_M = _today.getMonth();

const OFFICERS = ["Ma'am Maria","Ma'am Donna","Sir MJ"];
const CLIENT_CATEGORIES: Client["clientCategory"][] = ["Probationer","Parolee","Pardonee"];
const SUPERVISION_PHASES = ["Phase 1","Phase 2","Phase 3","Terminal"];
const AVAILABLE_YEARS = [2024,2025,2026,2027];
const PHASE_ORDER: Record<string,number> = {"Phase 1":1,"Phase 2":2,"Phase 3":3,"Terminal":4,"Completed":5};
const CIVIL_STATUSES = ["Single","Married","Widowed","Separated","Annulled"];
const EDUCATION_LEVELS = ["Elementary","High School","Vocational","College","Post-Graduate"];

// ── Sample Data ────────────────────────────────────────────────────────────────
const _pp = (overrides: Partial<Client>): Omit<Client,"clientId"|"photoInitial"> => ({
  docketNumber:"",fullName:"",middleInitial:"",gender:"Male",clientCategory:"Probationer",
  ccNumber:"",court:"",doNdo:"DO",assignedOfficer:"",supervisionStart:"",supervisionEnd:"",
  supervisionPhase:"Phase 1",registrationDate:"2021-01-01",status:"Active",caseType:"Non-Drug",remarks:"",
  finalReport:"",finalReportDate:"",terminationDate:"",violationReport:"",violationDate:"",
  courtOrderDisposition:"",courtOrderDateSubmitted:"",courtOrderDateReceived:"",
  fingerprintId:"",fingerprintEnrolled:false,fingerprintEnrollmentDate:"",fingerprintImage:"",
  piNumber:"",alias:"",identifyingMarks:"",address:"",barangay:"",contactNumber:"",
  dateOfBirth:"",placeOfBirth:"",civilStatus:"Single",spouseName:"",numberOfDependents:"0",
  educationalAttainment:"",occupation:"",monthlyIncome:"",hobbies:"",skills:"",religiousAffiliation:"",
  psiNumber:"",criminalCaseNumber:"",chargedWith:"",dateCommitted:"",convictedOf:"",dateConvicted:"",
  sentence:"",placeOfReferral:"",datePsiSubmitted:"",custodyStatus:"",dateProbationGranted:"",
  dateProbationOrderReceived:"",periodOfProbation:"",dateFrSubmitted:"",dateOfToro:"",
  dateReceivedCase:"",investigatingOfficer:"",
  ...overrides,
});

const INITIAL_CLIENTS: Client[] = [
  {clientId:"TPP-0001",photoInitial:"R",..._pp({docketNumber:"DOC-2021-045",fullName:"Rodrigo Manalac",middleInitial:"T.",gender:"Male",clientCategory:"Probationer",ccNumber:"CC-2021-001",court:"RTC Branch 18, Tagaytay City",doNdo:"DO",assignedOfficer:OFFICERS[0],supervisionStart:"2021-03-12",supervisionEnd:"2027-03-12",supervisionPhase:"Phase 2",registrationDate:"2021-03-12",status:"Active",remarks:"Cooperative, reporting on schedule",fingerprintId:"FP-3821",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2021-03-15",piNumber:"PI-2021-001",alias:"Rods",address:"Brgy. Kaybagal North, Tagaytay City",barangay:"Kaybagal North",contactNumber:"09171234567",dateOfBirth:"1985-06-15",placeOfBirth:"Tagaytay City",civilStatus:"Married",spouseName:"Maria Manalac",numberOfDependents:"2",educationalAttainment:"College",occupation:"Carpenter",monthlyIncome:"12000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2021-045",criminalCaseNumber:"Crim. Case No. 2021-045",chargedWith:"Qualified Theft",dateCommitted:"2020-08-10",convictedOf:"Theft",sentence:"3 years probation",dateProbationGranted:"2021-03-12",periodOfProbation:"6 years",investigatingOfficer:OFFICERS[0]})},
  {clientId:"TPP-0002",photoInitial:"B",..._pp({docketNumber:"DOC-2020-112",fullName:"Bernardo Sison",middleInitial:"P.",gender:"Male",clientCategory:"Parolee",ccNumber:"CC-2020-019",court:"RTC Branch 19, Tagaytay City",doNdo:"NDO",assignedOfficer:OFFICERS[1],supervisionStart:"2020-07-19",supervisionEnd:"2025-07-19",supervisionPhase:"Phase 3",registrationDate:"2020-07-19",status:"Active",remarks:"Missed Feb — notified via contact number",fingerprintId:"FP-4432",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2020-07-22",piNumber:"PI-2020-019",alias:"Bernie",address:"Brgy. Sungay West, Tagaytay City",barangay:"Sungay West",contactNumber:"09281234567",dateOfBirth:"1978-11-02",placeOfBirth:"Batangas City",civilStatus:"Married",spouseName:"Josefa Sison",numberOfDependents:"3",educationalAttainment:"High School",occupation:"Farmer",monthlyIncome:"8000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2020-112",criminalCaseNumber:"Crim. Case No. 2020-112",chargedWith:"Robbery",convictedOf:"Robbery",sentence:"Paroled after 3 years",dateProbationGranted:"2020-07-19",periodOfProbation:"5 years",investigatingOfficer:OFFICERS[1]})},
  {clientId:"TPP-0003",photoInitial:"E",..._pp({docketNumber:"DOC-2022-078",fullName:"Elena Ramos",middleInitial:"B.",gender:"Female",clientCategory:"Probationer",ccNumber:"CC-2022-008",court:"MTC Branch 1, Tagaytay City",doNdo:"DO",assignedOfficer:OFFICERS[0],supervisionStart:"2022-01-05",supervisionEnd:"2027-01-05",supervisionPhase:"Phase 2",registrationDate:"2022-01-05",status:"Active",remarks:"",fingerprintId:"FP-5573",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2022-01-08",piNumber:"PI-2022-008",alias:"",address:"Brgy. Maharlika West, Tagaytay City",barangay:"Maharlika West",contactNumber:"09391234567",dateOfBirth:"1992-03-28",placeOfBirth:"Tagaytay City",civilStatus:"Single",spouseName:"",numberOfDependents:"0",educationalAttainment:"College",occupation:"Vendor",monthlyIncome:"7000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2022-078",criminalCaseNumber:"Crim. Case No. 2022-078",chargedWith:"Estafa",convictedOf:"Estafa",sentence:"3 years probation",dateProbationGranted:"2022-01-05",periodOfProbation:"5 years",investigatingOfficer:OFFICERS[0]})},
  {clientId:"TPP-0004",photoInitial:"D",..._pp({docketNumber:"DOC-2019-230",fullName:"Danilo Reyes",middleInitial:"G.",gender:"Male",clientCategory:"Parolee",ccNumber:"CC-2019-047",court:"RTC Branch 18, Tagaytay City",doNdo:"NDO",assignedOfficer:OFFICERS[2],supervisionStart:"2019-11-28",supervisionEnd:"2024-11-28",supervisionPhase:"Phase 3",registrationDate:"2019-11-28",status:"Active",remarks:"Supervision period ended Nov 2024 — review pending",fingerprintId:"FP-6614",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2019-12-01",piNumber:"PI-2019-047",alias:"Dan",address:"Brgy. Calabuso, Tagaytay City",barangay:"Calabuso",contactNumber:"09451234567",dateOfBirth:"1975-08-14",placeOfBirth:"Cavite City",civilStatus:"Married",spouseName:"Rosa Reyes",numberOfDependents:"4",educationalAttainment:"High School",occupation:"Construction Worker",monthlyIncome:"10000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2019-230",criminalCaseNumber:"Crim. Case No. 2019-230",chargedWith:"Homicide",convictedOf:"Homicide (mitigating)",sentence:"Paroled after 5 years",dateProbationGranted:"2019-11-28",periodOfProbation:"5 years",investigatingOfficer:OFFICERS[2]})},
  {clientId:"TPP-0005",photoInitial:"N",..._pp({docketNumber:"DOC-2023-011",fullName:"Norma Castillo",middleInitial:"C.",gender:"Female",clientCategory:"Pardonee",ccNumber:"CC-2023-002",court:"RTC Branch 20, Tagaytay City",doNdo:"DO",assignedOfficer:OFFICERS[1],supervisionStart:"2023-04-14",supervisionEnd:"2028-04-14",supervisionPhase:"Phase 1",registrationDate:"2023-04-14",status:"Active",remarks:"",fingerprintId:"FP-7755",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2023-04-18",piNumber:"PI-2023-002",alias:"Norming",address:"Brgy. Asisan, Tagaytay City",barangay:"Asisan",contactNumber:"09561234567",dateOfBirth:"1988-12-01",placeOfBirth:"Tagaytay City",civilStatus:"Widowed",spouseName:"",numberOfDependents:"1",educationalAttainment:"Vocational",occupation:"Laundrywoman",monthlyIncome:"5000",religiousAffiliation:"Born Again Christian",psiNumber:"PSI-2023-011",criminalCaseNumber:"Crim. Case No. 2023-011",chargedWith:"Drug Possession",convictedOf:"Drug Possession (minor)",sentence:"Pardoned — executive clemency",dateProbationGranted:"2023-04-14",caseType:"Drug" as const,periodOfProbation:"5 years",investigatingOfficer:OFFICERS[1]})},
  {clientId:"TPP-0006",photoInitial:"F",..._pp({docketNumber:"DOC-2021-189",fullName:"Federico Luna",middleInitial:"M.",gender:"Male",clientCategory:"Probationer",ccNumber:"CC-2021-028",court:"MTC Branch 2, Tagaytay City",doNdo:"DO",assignedOfficer:OFFICERS[2],supervisionStart:"2021-09-02",supervisionEnd:"2026-07-20",supervisionPhase:"Phase 3",registrationDate:"2021-09-02",status:"Active",remarks:"Has pending fee payment",fingerprintId:"FP-8896",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2021-09-05",piNumber:"PI-2021-028",alias:"Fred",address:"Brgy. Sungay East, Tagaytay City",barangay:"Sungay East",contactNumber:"09671234567",dateOfBirth:"1981-04-17",placeOfBirth:"Tagaytay City",civilStatus:"Married",spouseName:"Leticia Luna",numberOfDependents:"2",educationalAttainment:"High School",occupation:"Tricycle Driver",monthlyIncome:"9000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2021-189",criminalCaseNumber:"Crim. Case No. 2021-189",chargedWith:"Physical Injuries",convictedOf:"Less Serious Physical Injuries",sentence:"2 years probation",dateProbationGranted:"2021-09-02",periodOfProbation:"5 years",investigatingOfficer:OFFICERS[2]})},
  {clientId:"TPP-0007",photoInitial:"C",..._pp({docketNumber:"DOC-2020-301",fullName:"Cynthia Valdez",middleInitial:"A.",gender:"Female",clientCategory:"Pardonee",ccNumber:"CC-2020-042",court:"RTC Branch 19, Tagaytay City",doNdo:"NDO",assignedOfficer:OFFICERS[2],supervisionStart:"2020-02-17",supervisionEnd:"2025-02-17",supervisionPhase:"Terminal",registrationDate:"2020-02-17",status:"Completed",remarks:"Supervision period completed Feb 2025",finalReport:"Final report submitted",finalReportDate:"2025-02-10",fingerprintId:"FP-9137",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2020-02-20",piNumber:"PI-2020-042",alias:"Cynth",address:"Brgy. Kaybagal South, Tagaytay City",barangay:"Kaybagal South",contactNumber:"09781234567",dateOfBirth:"1970-07-22",placeOfBirth:"Manila",civilStatus:"Separated",spouseName:"",numberOfDependents:"2",educationalAttainment:"College",occupation:"Seamstress",monthlyIncome:"6000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2020-301",criminalCaseNumber:"Crim. Case No. 2020-301",chargedWith:"Executive Clemency",convictedOf:"Homicide",sentence:"Pardoned",dateProbationGranted:"2020-02-17",periodOfProbation:"5 years",investigatingOfficer:OFFICERS[2]})},
  {clientId:"TPP-0008",photoInitial:"A",..._pp({docketNumber:"DOC-2022-144",fullName:"Armando Cruz",middleInitial:"R.",gender:"Male",clientCategory:"Probationer",ccNumber:"CC-2022-022",court:"RTC Branch 18, Tagaytay City",doNdo:"DO",assignedOfficer:OFFICERS[1],supervisionStart:"2022-06-30",supervisionEnd:"2027-06-30",supervisionPhase:"Phase 2",registrationDate:"2022-06-30",status:"Active",remarks:"Missed Feb and Mar — follow-up required",fingerprintId:"FP-2278",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2022-07-03",piNumber:"PI-2022-022",alias:"Anding",address:"Brgy. Maitim 2nd East, Tagaytay City",barangay:"Maitim 2nd East",contactNumber:"09891234567",dateOfBirth:"1990-01-30",placeOfBirth:"Tagaytay City",civilStatus:"Single",spouseName:"",numberOfDependents:"0",educationalAttainment:"High School",occupation:"Security Guard",monthlyIncome:"13000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2022-144",criminalCaseNumber:"Crim. Case No. 2022-144",chargedWith:"Malversation",convictedOf:"Malversation (minor)",sentence:"3 years probation",dateProbationGranted:"2022-06-30",periodOfProbation:"5 years",investigatingOfficer:OFFICERS[1]})},
  {clientId:"TPP-0009",photoInitial:"L",..._pp({docketNumber:"DOC-2023-055",fullName:"Lourdes Mendoza",middleInitial:"F.",gender:"Female",clientCategory:"Parolee",ccNumber:"CC-2023-009",court:"MTC Branch 1, Tagaytay City",doNdo:"DO",assignedOfficer:OFFICERS[0],supervisionStart:"2023-08-22",supervisionEnd:"2028-08-22",supervisionPhase:"Phase 1",registrationDate:"2023-08-22",status:"Active",remarks:"",fingerprintId:"FP-1199",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2023-08-25",piNumber:"PI-2023-009",alias:"",address:"Brgy. Tolentino West, Tagaytay City",barangay:"Tolentino West",contactNumber:"09101234567",dateOfBirth:"1995-05-10",placeOfBirth:"Tagaytay City",civilStatus:"Single",spouseName:"",numberOfDependents:"0",educationalAttainment:"College",occupation:"Sales Associate",monthlyIncome:"11000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2023-055",criminalCaseNumber:"Crim. Case No. 2023-055",chargedWith:"Robbery",convictedOf:"Robbery",sentence:"Paroled after 2 years",dateProbationGranted:"2023-08-22",periodOfProbation:"5 years",investigatingOfficer:OFFICERS[0]})},
  {clientId:"TPP-0010",photoInitial:"R",..._pp({docketNumber:"DOC-2021-367",fullName:"Renato Aguilar",middleInitial:"B.",gender:"Male",clientCategory:"Probationer",ccNumber:"CC-2021-055",court:"RTC Branch 20, Tagaytay City",doNdo:"NDO",assignedOfficer:OFFICERS[1],supervisionStart:"2021-12-01",supervisionEnd:"2026-12-01",supervisionPhase:"Phase 2",registrationDate:"2021-12-01",status:"Active",remarks:"",fingerprintId:"FP-3310",fingerprintEnrolled:true,fingerprintEnrollmentDate:"2021-12-05",piNumber:"PI-2021-055",alias:"Ato",address:"Brgy. Francisco, Tagaytay City",barangay:"Francisco",contactNumber:"09211234567",dateOfBirth:"1983-09-19",placeOfBirth:"Cavite City",civilStatus:"Married",spouseName:"Gina Aguilar",numberOfDependents:"3",educationalAttainment:"High School",occupation:"Jeepney Driver",monthlyIncome:"14000",religiousAffiliation:"Roman Catholic",psiNumber:"PSI-2021-367",criminalCaseNumber:"Crim. Case No. 2021-367",chargedWith:"Violation of RA 9165",convictedOf:"Drug Use",sentence:"3 years probation",dateProbationGranted:"2021-12-01",caseType:"Drug" as const,periodOfProbation:"5 years",investigatingOfficer:OFFICERS[1]})},
];

const MONTHLY_PRESENCE: Record<string,boolean[]> = {
  "TPP-0001":[true,true,true,true,true,true,false,false,false,false,false,false],
  "TPP-0002":[true,false,true,true,true,true,false,false,false,false,false,false],
  "TPP-0003":[true,true,false,true,true,true,false,false,false,false,false,false],
  "TPP-0004":[false,true,true,false,true,true,false,false,false,false,false,false],
  "TPP-0005":[true,true,true,true,false,true,false,false,false,false,false,false],
  "TPP-0006":[true,true,true,true,true,false,false,false,false,false,false,false],
  "TPP-0007":[false,false,false,false,false,false,false,false,false,false,false,false],
  "TPP-0008":[true,false,false,true,true,true,false,false,false,false,false,false],
  "TPP-0009":[true,true,true,true,true,true,false,false,false,false,false,false],
  "TPP-0010":[true,true,true,false,true,true,false,false,false,false,false,false],
};

const REPORT_DAYS = [5,8,10,7,12,9,6,11,14,8,5,12];
const REPORT_TIMES = ["08:05","08:22","09:10","08:45","08:15","09:30","08:00","08:50","09:05","08:35"];

function buildInitialAttendance(): AttendanceRecord[] {
  const recs: AttendanceRecord[] = [];
  INITIAL_CLIENTS.forEach((c,ci)=>{
    (MONTHLY_PRESENCE[c.clientId]??[]).forEach((present,mi)=>{
      if (!present) return;
      recs.push({
        attendanceId:`ATT-${c.clientId}-${YEAR}${String(mi+1).padStart(2,"0")}`,
        clientId:c.clientId, fullName:c.fullName, caseNumber:c.ccNumber,
        docketNumber:c.docketNumber,
        date:`${YEAR}-${String(mi+1).padStart(2,"0")}-${String(REPORT_DAYS[mi]).padStart(2,"0")}`,
        time:REPORT_TIMES[ci % REPORT_TIMES.length], verifiedBy:c.assignedOfficer,
      });
    });
  });
  return recs;
}

// ── Helpers ────────────────────────────────────────────────────────────────────
function cellStatus(client: Client, mi: number, recs: AttendanceRecord[], year: number = YEAR): "P"|"A"|"-" {
  const prefix = `${year}-${String(mi+1).padStart(2,"0")}`;
  const rec = recs.find(r=>r.clientId===client.clientId && r.date.startsWith(prefix));
  if (rec) {
    if (rec.status === "Absent") return "A";
    if (rec.status === "Blank") return "-";
    return "P"; // default Present
  }
  // Check if before supervision start
  if (client.supervisionStart) {
    const [sy, sm] = client.supervisionStart.split("-").map(Number);
    if (year < sy || (year === sy && mi + 1 < sm)) {
      return "-"; // Blank if before supervision start
    }
  }
  const curYear = _today.getFullYear();
  if (year < curYear) return "A";
  if (year === curYear && mi < CUR_M) return "A";
  return "-";
}

function supStatus(c: Client): "Active"|"Near Expiry"|"Ended"|"Completed"|"Terminated" {
  if (c.status==="Completed") return "Completed";
  if (c.status==="Terminated") return "Terminated";
  if (c.supervisionEnd < todayStr) return "Ended";
  const d = new Date(_today); d.setDate(d.getDate()+30);
  if (c.supervisionEnd <= d.toISOString().split("T")[0]) return "Near Expiry";
  return "Active";
}

function supStatusColor(st: string) {
  if (st==="Active") return "text-emerald-700 bg-emerald-50 border-emerald-300";
  if (st==="Near Expiry") return "text-amber-700 bg-amber-50 border-amber-300";
  if (st==="Ended") return "text-red-700 bg-red-50 border-red-300";
  if (st==="Completed") return "text-blue-700 bg-blue-50 border-blue-200";
  if (st==="Terminated") return "text-violet-700 bg-violet-50 border-violet-200";
  return "text-slate-600 bg-slate-100 border-slate-300";
}

function supStatusDot(st: string) {
  if (st==="Active") return "bg-emerald-500";
  if (st==="Near Expiry") return "bg-amber-400";
  if (st==="Ended") return "bg-red-500";
  if (st==="Completed") return "bg-blue-400";
  return "bg-slate-400";
}

function formatName(c: {fullName:string;middleInitial?:string}): string {
  const parts = c.fullName.trim().split(" ");
  if (parts.length < 2) return c.fullName;
  const last = parts[parts.length-1];
  const first = parts.slice(0,parts.length-1).join(" ");
  const mi = c.middleInitial ? ` ${c.middleInitial}` : "";
  return `${last}, ${first}${mi}`;
}

function pastWeekDates(): {label:string;date:string}[] {
  return Array.from({length:7},(_,i)=>{
    const d = new Date(_today); d.setDate(d.getDate()-i);
    const ds = d.toISOString().split("T")[0];
    const label = i===0?"Today":i===1?"Yesterday":d.toLocaleDateString("en-PH",{weekday:"short",month:"short",day:"numeric"});
    return {label,date:ds};
  });
}

function computeAge(dob: string): string {
  if (!dob) return "—";
  const d = new Date(dob);
  const age = Math.floor((_today.getTime()-d.getTime())/(1000*60*60*24*365.25));
  return isNaN(age) ? "—" : String(age);
}

// Dynamic monthly chart data function
function getMonthlyChartData(clients: Client[], recs: AttendanceRecord[], year: number) {
  const activeClients = clients.filter(c => c.status === "Active");
  const total = activeClients.length;
  const data = [];
  for (let mi = 0; mi < 12; mi++) {
    const prefix = `${year}-${String(mi+1).padStart(2,"0")}`;
    const attended = activeClients.filter(c => {
      const rec = recs.find(r => r.clientId === c.clientId && r.date.startsWith(prefix));
      return rec && rec.status !== "Absent" && rec.status !== "Blank";
    }).length;
    data.push({ month: MONTHS[mi], attended, total });
  }
  return data;
}

// ── Shared UI ─────────────────────────────────────────────────────────────────
function Badge({status}:{status:string}) {
  const map: Record<string,string> = {
    Probationer:"text-sky-700 bg-sky-50 border-sky-200",
    Parolee:"text-violet-700 bg-violet-50 border-violet-200",
    Pardonee:"text-orange-700 bg-orange-50 border-orange-200",
    Present:"text-emerald-700 bg-emerald-50 border-emerald-200",
    Absent:"text-red-700 bg-red-50 border-red-200",
    DO:"text-slate-700 bg-slate-100 border-slate-200",
    NDO:"text-indigo-700 bg-indigo-50 border-indigo-200",
    Male:"text-blue-700 bg-blue-50 border-blue-200",
    Female:"text-pink-700 bg-pink-50 border-pink-200",
  };
  return <span className={`text-[10px] font-mono px-2 py-0.5 rounded border whitespace-nowrap ${map[status]??"text-slate-600 bg-slate-100 border-slate-200"}`}>{status}</span>;
}

function SupBadge({client}:{client:Client}) {
  const st = supStatus(client);
  return <span className={`text-[10px] font-mono px-2 py-0.5 rounded border flex items-center gap-1 w-fit ${supStatusColor(st)}`}>
    <span className={`w-1.5 h-1.5 rounded-full flex-shrink-0 ${supStatusDot(st)}`} />{st}
  </span>;
}

function SortIcon({col,cur,dir}:{col:string;cur:string;dir:"asc"|"desc"}) {
  if (col!==cur) return <span className="text-muted-foreground/30 ml-0.5 text-[10px]">↕</span>;
  return <span className="text-sky-400 ml-0.5 text-[10px] font-bold">{dir==="asc"?"↑":"↓"}</span>;
}

// Unified sort bar — shared across Matrix, Search, Management
function SortBar<T extends string>({
  label="Sort by:", options, sortBy, sortDir, onToggle,
}: {
  label?: string;
  options: [T, string][];
  sortBy: T;
  sortDir: "asc"|"desc";
  onToggle: (col: T) => void;
}) {
  return (
    <div className="flex items-center gap-2 flex-wrap">
      <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest flex-shrink-0">{label}</span>
      <div className="flex items-center gap-1 flex-wrap">
        {options.map(([k, l]) => {
          const active = sortBy === k;
          return (
            <button key={k} onClick={() => onToggle(k)}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-[11px] font-semibold transition-all border-2 select-none ${
                active
                  ? "border-sky-500 bg-sky-500 text-white shadow-sm"
                  : "border-border bg-input-background text-muted-foreground hover:border-sky-300 hover:text-foreground"
              }`}>
              {l}
              {active && (
                <span className={`text-[11px] font-bold leading-none ${sortDir==="asc"?"":"rotate-180 inline-block"}`}>
                  {sortDir==="asc" ? "↑" : "↓"}
                </span>
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}

function LiveClock() {
  const [now,setNow] = useState(new Date());
  useEffect(()=>{const id=setInterval(()=>setNow(new Date()),1000);return()=>clearInterval(id);},[]);
  return (
    <div className="text-right hidden md:block">
      <div className="text-[10px] text-muted-foreground">{now.toLocaleDateString("en-PH",{weekday:"long",year:"numeric",month:"long",day:"numeric"})}</div>
      <div className="text-xs font-bold text-foreground font-mono">{now.toLocaleTimeString("en-PH",{hour:"2-digit",minute:"2-digit",second:"2-digit"})}</div>
    </div>
  );
}

function Topbar({title,sub}:{title:string;sub?:string}) {
  return (
    <header className="h-14 flex items-center justify-between px-6 bg-card border-b border-border flex-shrink-0">
      <div>
        <h1 className="text-sm font-bold text-foreground leading-tight">{title}</h1>
        {sub && <p className="text-[11px] text-muted-foreground">{sub}</p>}
      </div>
      <div className="flex items-center gap-4">
        <LiveClock />
        <button className="relative p-1.5 rounded hover:bg-muted transition-colors">
          <Bell className="w-4 h-4 text-muted-foreground" />
          <span className="absolute top-0.5 right-0.5 w-2 h-2 bg-sky-500 rounded-full" />
        </button>
      </div>
    </header>
  );
}

function Sidebar({page,setPage,onLogout,collapsed,setCollapsed}:{
  page:Page;setPage:(p:Page)=>void;onLogout:()=>void;collapsed:boolean;setCollapsed:(v:boolean)=>void;
}) {
  const nav:{id:Page;label:string;icon:React.ElementType}[] = [
    {id:"dashboard",label:"Dashboard",icon:LayoutDashboard},
    {id:"attendance",label:"Attendance",icon:CalendarCheck},
    {id:"history",label:"Monthly Matrix",icon:History},
    {id:"search",label:"Database / Search",icon:Database},
    {id:"reports",label:"Reports",icon:BarChart2},
    {id:"management",label:"Client Management",icon:Users2},
  ];
  return (
    <aside className={`${collapsed?"w-14":"w-64"} flex-shrink-0 flex flex-col overflow-hidden transition-all duration-200`} style={{background:"var(--sidebar)"}}>
      {!collapsed && (
        <div className="px-5 py-4 border-b" style={{borderColor:"var(--sidebar-border)"}}>
          <div className="flex items-center gap-2.5 mb-2">
            <div className="w-9 h-9 rounded-lg bg-sky-500 flex items-center justify-center flex-shrink-0"><Shield className="w-4 h-4 text-white" /></div>
            <div>
              <div className="text-[11px] font-bold text-white leading-tight">Tagaytay Parole &amp;</div>
              <div className="text-[11px] font-bold text-white leading-tight">Probation Office</div>
            </div>
          </div>
          <div className="text-[9px] font-mono px-2 py-0.5 rounded inline-block" style={{background:"rgba(255,255,255,0.08)",color:"rgba(255,255,255,0.4)"}}>Attendance Monitoring System</div>
        </div>
      )}
      {collapsed && <div className="py-4 flex justify-center border-b" style={{borderColor:"var(--sidebar-border)"}}><Shield className="w-5 h-5 text-sky-400" /></div>}
      <nav className="flex-1 py-3 px-2 space-y-0.5 overflow-y-auto">
        {nav.map(({id,label,icon:Icon})=>{
          const active=page===id;
          return (
            <button key={id} onClick={()=>setPage(id)} title={collapsed?label:undefined}
              className="w-full flex items-center gap-3 px-2.5 py-2.5 rounded text-xs font-semibold transition-all"
              style={{background:active?"var(--sidebar-accent)":"transparent",color:active?"#fff":"rgba(255,255,255,0.65)"}}>
              <Icon className="w-4 h-4 flex-shrink-0" />
              {!collapsed && <>{label}{active && <div className="ml-auto w-1 h-4 rounded-full bg-sky-400" />}</>}
            </button>
          );
        })}
      </nav>
      <div className="px-2 pb-3 pt-2 border-t" style={{borderColor:"var(--sidebar-border)"}}>
        {!collapsed && (
          <div className="px-2.5 py-2 flex items-center gap-2 mb-1">
            <div className="w-7 h-7 rounded-full bg-sky-600 flex items-center justify-center text-xs font-bold text-white flex-shrink-0">A</div>
            <div className="flex-1 min-w-0">
              <div className="text-xs font-semibold text-white truncate">Admin User</div>
              <div className="text-[9px] font-mono" style={{color:"rgba(255,255,255,0.4)"}}>Administrator</div>
            </div>
          </div>
        )}
        <button onClick={()=>setCollapsed(!collapsed)} title={collapsed?"Expand sidebar":"Collapse sidebar"}
          className="w-full flex items-center justify-center gap-2 px-2.5 py-2 rounded text-xs font-semibold transition-all border hover:bg-white/15 mb-1"
          style={{color:"rgba(255,255,255,0.75)",borderColor:"rgba(255,255,255,0.15)"}}>
          {collapsed ? <ChevronsRight className="w-4 h-4" /> : <><ChevronsLeft className="w-4 h-4" /><span>Collapse</span></>}
        </button>
        <button onClick={onLogout} title={collapsed?"Sign Out":undefined}
          className="w-full flex items-center gap-2.5 px-2.5 py-2 rounded text-xs transition-all hover:bg-red-900/30"
          style={{color:"rgba(255,255,255,0.55)"}}>
          <LogOut className="w-3.5 h-3.5 flex-shrink-0" />{!collapsed && "Sign Out"}
        </button>
      </div>
    </aside>
  );
}

// ── Client Info Modal (enhanced — 4 tabs) ──────────────────────────────────────
type InfoTab = "summary"|"personal"|"case"|"fingerprint";

function ClientInfoModal({client,recs,onClose,onEdit,updateRec,addRec}:{client:Client;recs:AttendanceRecord[];onClose:()=>void;onEdit?:()=>void;updateRec?:(id:string,st:string)=>void;addRec?:(r:AttendanceRecord)=>void}) {
  const [tab,setTab] = useState<InfoTab>("summary");
  const [fpAction,setFpAction] = useState<string|null>(null);
  const [fpStatus,setFpStatus] = useState<string|null>(null);
  const [fpError,setFpError] = useState<string|null>(null);

  const handleFpConfirm = () => {
    if (!fpAction) return;
    setFpStatus("connecting");
    setFpError(null);
    const socket = new WebSocket("ws://localhost:5000/");
    socket.onopen = () => {
      socket.send(JSON.stringify({
        action: fpAction === "remove" ? "remove_fingerprint" : "enroll_fingerprint",
        clientId: client.clientId
      }));
    };
    socket.onmessage = (e) => {
      const res = JSON.parse(e.data);
      if (res.status) {
        setFpStatus(res.status);
      } else if (res.success) {
        setFpStatus("success");
        alert("Fingerprint successfully " + (fpAction === "remove" ? "removed" : "enrolled") + " for " + client.fullName);
        window.dispatchEvent(new Event("refreshData"));
        setTimeout(() => setFpStatus(null), 1500);
      } else {
        setFpError(res.error || "An unknown error occurred.");
        setFpStatus(null);
      }
    };
    socket.onerror = () => {
      setFpError("Failed to connect to scanner service.");
      setFpStatus(null);
    };
  };
  const clientRecs = recs.filter(r=>r.clientId===client.clientId);
  const st = supStatus(client);
  const curMonthPrefix = `${YEAR}-${String(CUR_M+1).padStart(2,"0")}`;
  const attendedThisMonth = clientRecs.some(r=>r.date.startsWith(curMonthPrefix));

  // Years to show in supervision history
  const supStartYear = client.supervisionStart ? parseInt(client.supervisionStart.slice(0,4)) : _today.getFullYear();
  const supEndYear = client.supervisionEnd ? parseInt(client.supervisionEnd.slice(0,4)) : _today.getFullYear();
  const maxYear = Math.max(_today.getFullYear(), supEndYear);
  const matrixYears = Array.from({length:maxYear-supStartYear+1},(_,i)=>supStartYear+i);

  const tabs: {id:InfoTab;label:string;icon:React.ElementType}[] = [
    {id:"summary",label:"Summary",icon:User},
    {id:"personal",label:"Personal Profile",icon:BookOpen},
    {id:"case",label:"Case Profile",icon:Briefcase},
    {id:"fingerprint",label:"Fingerprint",icon:Fingerprint},
  ];

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-3" onClick={onClose}>
      <div className="bg-card rounded-lg border border-border w-full max-w-2xl max-h-[94vh] overflow-hidden flex flex-col" onClick={e=>e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between flex-shrink-0" style={{background:"var(--primary)"}}>
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl flex items-center justify-center text-lg font-bold text-white flex-shrink-0"
              style={{background:"rgba(255,255,255,0.15)"}}>{client.photoInitial}</div>
            <div>
              <h2 className="text-base font-bold text-white leading-tight">{formatName(client)}</h2>
              <div className="flex items-center gap-1.5 mt-1 flex-wrap">
                <Badge status={client.clientCategory} />
                <Badge status={client.gender} />
                <Badge status={client.doNdo} />
                <SupBadge client={client} />
              </div>
            </div>
          </div>
          <button onClick={onClose} className="text-white/60 hover:text-white p-1"><X className="w-5 h-5" /></button>
        </div>

        {/* Tab buttons */}
        <div className="flex border-b border-border flex-shrink-0 bg-card">
          {tabs.map(({id,label,icon:Icon})=>(
            <button key={id} onClick={()=>setTab(id)}
              className={`flex-1 flex items-center justify-center gap-1.5 py-2.5 text-[11px] font-semibold transition-colors border-b-2 ${tab===id?"border-sky-500 text-sky-600 bg-sky-50/50":"border-transparent text-muted-foreground hover:text-foreground"}`}>
              <Icon className="w-3.5 h-3.5" />{label}
            </button>
          ))}
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-5">

          {/* ── Tab: Summary ─────────────────────── */}
          {tab==="summary" && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-2">
                {[
                  ["Client ID",client.clientId],["Docket No.",client.docketNumber],
                  ["CC Number",client.ccNumber||"—"],["Court",client.court||"—"],
                  ["Supervising Officer",client.assignedOfficer],["Phase",client.supervisionPhase],
                  ["DO / NDO",client.doNdo],["Contact",client.contactNumber||"—"],
                ].map(([k,v])=>(
                  <div key={k} className="bg-muted rounded p-2.5">
                    <div className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">{k}</div>
                    <div className="text-xs font-semibold text-foreground">{v}</div>
                  </div>
                ))}
              </div>
              <div className={`rounded border p-3 ${supStatusColor(st)}`}>
                <div className="flex items-center justify-between mb-1">
                  <span className="text-[9px] font-mono uppercase tracking-widest">Supervision Status</span>
                  <span className="text-xs font-bold">{st}</span>
                </div>
                <div className="flex items-center gap-3 text-xs font-mono">
                  <span className="opacity-70">Start:</span><strong>{client.supervisionStart}</strong>
                  <span className="opacity-70">→ End:</span><strong>{client.supervisionEnd}</strong>
                </div>
              </div>
              <div className="flex items-center justify-between bg-muted rounded p-3">
                <div className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground">This Month Attendance</div>
                {attendedThisMonth
                  ? <span className="text-emerald-700 text-xs font-bold flex items-center gap-1"><CheckCircle2 className="w-4 h-4" />Attended</span>
                  : <span className="text-amber-700 text-xs font-bold flex items-center gap-1"><Clock className="w-4 h-4" />Not Yet Reported</span>}
              </div>
              {client.remarks && (
                <div className="bg-amber-50 border border-amber-200 rounded p-3">
                  <div className="text-[9px] font-mono uppercase tracking-widest text-amber-700 mb-1">Remarks</div>
                  <p className="text-xs text-amber-900">{client.remarks}</p>
                </div>
              )}
              {(client.finalReport||client.violationReport||client.terminationDate) && (
                <div className="space-y-2">
                  <p className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground">Structured Remarks</p>
                  {client.finalReport && <div className="bg-muted rounded p-2 text-xs"><span className="text-muted-foreground font-mono">Final Report: </span>{client.finalReport}{client.finalReportDate && ` (${client.finalReportDate})`}</div>}
                  {client.violationReport && <div className="bg-red-50 border border-red-200 rounded p-2 text-xs"><span className="text-red-700 font-mono">Violation: </span>{client.violationReport}{client.violationDate && ` (${client.violationDate})`}</div>}
                  {client.terminationDate && <div className="bg-muted rounded p-2 text-xs"><span className="text-muted-foreground font-mono">Termination Date: </span>{client.terminationDate}</div>}
                </div>
              )}
              {/* 2026 Monthly quick view */}
              <div>
                <p className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-2">{YEAR} Monthly Attendance</p>
                <div className="grid grid-cols-6 gap-1">
                  {MONTHS.map((m,mi)=>{
                    const st2 = cellStatus(client,mi,clientRecs,YEAR);
                    return (
                      <div key={m} className={`rounded p-1.5 text-center ${st2==="P"?"bg-emerald-100 border border-emerald-300":st2==="A"?"bg-red-100 border border-red-200":"bg-muted border border-border"}`}>
                        <div className="text-[9px] font-mono text-muted-foreground">{m}</div>
                        <div className={`text-[10px] font-bold mt-0.5 ${st2==="P"?"text-emerald-700":st2==="A"?"text-red-600":"text-muted-foreground"}`}>{st2}</div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          )}

          {/* ── Tab: Personal Profile ─────────────── */}
          {tab==="personal" && (
            <div className="space-y-4">
              <div className="flex items-center justify-between mb-1">
                <p className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground">PPA Form 12 — Personal Information</p>
                {onEdit && <button onClick={onEdit} className="flex items-center gap-1 text-[11px] text-sky-600 hover:underline font-semibold"><Edit2 className="w-3 h-3" />Edit</button>}
              </div>
              <div className="grid grid-cols-2 gap-2">
                {[
                  ["PI Number",client.piNumber||"—"],["Alias",client.alias||"—"],
                  ["Identifying Marks",client.identifyingMarks||"—"],["Contact Number",client.contactNumber||"—"],
                  ["Date of Birth",client.dateOfBirth||"—"],["Age",computeAge(client.dateOfBirth)],
                  ["Place of Birth",client.placeOfBirth||"—"],["Civil Status",client.civilStatus||"—"],
                  ["Spouse's Name",client.spouseName||"—"],["No. of Dependents",client.numberOfDependents||"0"],
                  ["Educational Attainment",client.educationalAttainment||"—"],["Occupation",client.occupation||"—"],
                  ["Monthly Income",client.monthlyIncome ? `₱${client.monthlyIncome}` : "—"],["Religious Affiliation",client.religiousAffiliation||"—"],
                ].map(([k,v])=>(
                  <div key={k} className="bg-muted rounded p-2.5">
                    <div className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">{k}</div>
                    <div className="text-xs font-semibold text-foreground">{v}</div>
                  </div>
                ))}
              </div>
              <div className="bg-muted rounded p-2.5">
                <div className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">Address</div>
                <div className="text-xs font-semibold text-foreground">{client.address || "—"}{client.barangay && `, ${client.barangay}`}</div>
              </div>
              {(client.hobbies||client.skills) && (
                <div className="grid grid-cols-2 gap-2">
                  <div className="bg-muted rounded p-2.5"><div className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">Hobbies</div><div className="text-xs text-foreground">{client.hobbies||"—"}</div></div>
                  <div className="bg-muted rounded p-2.5"><div className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">Skills</div><div className="text-xs text-foreground">{client.skills||"—"}</div></div>
                </div>
              )}
            </div>
          )}

          {/* ── Tab: Case Profile ────────────────── */}
          {tab==="case" && (
            <div className="space-y-4">
              <div className="flex items-center justify-between mb-1">
                <p className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground">PPA Form 12 — Case Information</p>
                {onEdit && <button onClick={onEdit} className="flex items-center gap-1 text-[11px] text-sky-600 hover:underline font-semibold"><Edit2 className="w-3 h-3" />Edit</button>}
              </div>
              <div className="grid grid-cols-2 gap-2">
                {[
                  ["PSI / PRPD Number",client.psiNumber||"—"],["Criminal Case Number",client.criminalCaseNumber||"—"],
                  ["Charged With",client.chargedWith||"—"],["Date Committed",client.dateCommitted||"—"],
                  ["Convicted Of",client.convictedOf||"—"],["Date Convicted",client.dateConvicted||"—"],
                  ["Sentence",client.sentence||"—"],["Court",client.court||"—"],
                  ["Place of Referral",client.placeOfReferral||"—"],["Date PSI Submitted",client.datePsiSubmitted||"—"],
                  ["Custody Status",client.custodyStatus||"—"],["Date Probation Granted",client.dateProbationGranted||"—"],
                  ["Date Order Received",client.dateProbationOrderReceived||"—"],["Period of Probation",client.periodOfProbation||"—"],
                  ["Supervision Start",client.supervisionStart||"—"],["Supervision End",client.supervisionEnd||"—"],
                  ["Date FR/SR/RR/TR Submitted",client.dateFrSubmitted||"—"],["Date of TORO",client.dateOfToro||"—"],
                  ["Date Received",client.dateReceivedCase||"—"],["Investigating Officer",client.investigatingOfficer||"—"],
                  ["Supervising Officer",client.assignedOfficer||"—"],["",""],
                ].filter(([,v])=>v!=="").map(([k,v])=>k ? (
                  <div key={k} className="bg-muted rounded p-2.5">
                    <div className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">{k}</div>
                    <div className="text-xs font-semibold text-foreground">{v}</div>
                  </div>
                ) : <div key="spacer" />)}
              </div>
              {/* Supervision History Matrix */}
              <div>
                <p className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-2">Supervision History Matrix (Read-Only)</p>
                <div className="overflow-x-auto rounded border border-border">
                  <table className="text-[10px] w-full">
                    <thead>
                      <tr className="bg-muted/50 border-b border-border">
                        <th className="text-left px-3 py-2 font-bold text-muted-foreground uppercase tracking-wide whitespace-nowrap">Month</th>
                        {matrixYears.map(y=><th key={y} className="text-center px-3 py-2 font-bold text-muted-foreground uppercase tracking-wide">{y}</th>)}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {MONTHS.map((m,mi)=>(
                        <tr key={m} className="hover:bg-muted/20">
                          <td className="px-3 py-1.5 font-mono font-semibold text-foreground">{m}</td>
                          {matrixYears.map(y=>{
                            const st3 = cellStatus(client,mi,clientRecs,y);
                            const toggleStatus = () => {
                              if (!addRec || !updateRec) return;
                              const prefix = `${y}-${String(mi+1).padStart(2,"0")}`;
                              const rec = clientRecs.find(r=>r.date.startsWith(prefix));
                              if (rec) {
                                if (rec.status === "Present" || !rec.status) updateRec(rec.attendanceId, "Absent");
                                else if (rec.status === "Absent") updateRec(rec.attendanceId, "Blank");
                                else updateRec(rec.attendanceId, "Present");
                              } else {
                                const newDate = `${prefix}-01`;
                                const now = new Date();
                                const t = `${String(now.getHours()).padStart(2,"0")}:${String(now.getMinutes()).padStart(2,"0")}`;
                                addRec({attendanceId:`ATT-MATRIX-${Date.now()}`,clientId:client.clientId,fullName:client.fullName,
                                  caseNumber:client.ccNumber,docketNumber:client.docketNumber,date:newDate,time:t,verifiedBy:"Matrix Edit",status:"Present"});
                              }
                            };
                            return (
                              <td key={y} className="px-3 py-1.5 text-center">
                                <button onClick={toggleStatus} disabled={!addRec} className={`w-full h-full min-w-[24px] min-h-[24px] flex items-center justify-center mx-auto ${addRec?"cursor-pointer hover:opacity-80":""}`}>
                                  {st3==="P" && <span className="w-5 h-5 rounded bg-emerald-100 border border-emerald-300 flex items-center justify-center text-emerald-700 font-bold">P</span>}
                                  {st3==="A" && <span className="w-5 h-5 rounded bg-red-100 border border-red-200 flex items-center justify-center text-red-600 font-bold">A</span>}
                                  {st3==="-" && <span className="text-muted-foreground font-mono">—</span>}
                                </button>
                              </td>
                            );
                          })}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}

          {/* ── Tab: Fingerprint ─────────────────── */}
          {tab==="fingerprint" && (
            <div className="space-y-4">
              <div className="flex items-center gap-4 bg-muted rounded p-4">
                <div className={`w-14 h-14 rounded-xl flex items-center justify-center flex-shrink-0 ${client.fingerprintEnrolled?"bg-emerald-100":"bg-muted-foreground/10"}`}>
                  <Fingerprint className={`w-8 h-8 ${client.fingerprintEnrolled?"text-emerald-600":"text-muted-foreground"}`} />
                </div>
                <div>
                  <p className="text-sm font-bold text-foreground">{client.fingerprintEnrolled?"Fingerprint Enrolled":"Not Enrolled"}</p>
                  {client.fingerprintId && <p className="text-[11px] font-mono text-muted-foreground mt-0.5">FP ID: {client.fingerprintId}</p>}
                  {client.fingerprintEnrollmentDate && <p className="text-[11px] text-muted-foreground">Enrolled: {client.fingerprintEnrollmentDate}</p>}
                  {!client.fingerprintEnrolled && <p className="text-xs text-amber-700 mt-1">This client cannot use fingerprint attendance until enrolled.</p>}
                </div>
              </div>
              {client.fingerprintEnrolled && client.fingerprintImage && (
                <div className="bg-card border rounded p-4 flex flex-col items-center">
                  <p className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground mb-3">Enrolled Fingerprint Image</p>
                  <div className="w-32 h-40 bg-white border border-border p-1 rounded-sm shadow-sm flex items-center justify-center">
                    <img src={`data:image/png;base64,${client.fingerprintImage}`} alt="Fingerprint" className="w-full h-full object-contain filter contrast-125 grayscale" />
                  </div>
                </div>
              )}
              <div className="space-y-2">
                <p className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground">Fingerprint Actions</p>
                {!client.fingerprintEnrolled && (
                  <button onClick={()=>setFpAction("register")}
                    className="w-full py-2.5 rounded text-xs font-bold text-white hover:opacity-90" style={{background:"var(--primary)"}}>
                    <Fingerprint className="w-3.5 h-3.5 inline mr-1.5" />Register Fingerprint
                  </button>
                )}
                {client.fingerprintEnrolled && (
                  <>
                    <button onClick={()=>setFpAction("update")}
                      className="w-full py-2.5 rounded text-xs font-bold border border-border text-foreground hover:bg-muted">
                      <RefreshCw className="w-3.5 h-3.5 inline mr-1.5" />Update Fingerprint
                    </button>
                    <button onClick={()=>setFpAction("remove")}
                      className="w-full py-2.5 rounded text-xs font-bold border border-red-200 text-red-600 bg-red-50 hover:bg-red-100">
                      <X className="w-3.5 h-3.5 inline mr-1.5" />Remove Fingerprint
                    </button>
                  </>
                )}
              </div>
              {fpAction && (
                <div className={`rounded border p-4 ${fpAction==="remove"?"bg-red-50 border-red-200":"bg-sky-50 border-sky-200"}`}>
                  <p className="text-xs font-semibold mb-2">
                    {fpAction==="register" && "Place client's right thumb on the Futronic FS64 scanner to register."}
                    {fpAction==="update" && "Place client's right thumb on the scanner to update the fingerprint template."}
                    {fpAction==="remove" && "Confirm removal of the enrolled fingerprint template for this client."}
                  </p>
                  
                  {fpError && <div className="text-xs text-red-600 bg-red-100 p-2 rounded mb-2 border border-red-200">{fpError}</div>}
                  
                  {fpStatus ? (
                    <div className="flex items-center justify-center p-2 text-xs font-bold text-sky-700 bg-sky-100 rounded border border-sky-200">
                      {fpStatus === "success" ? "Success! Reloading..." : 
                       fpStatus === "connecting" ? "Connecting to scanner..." :
                       fpStatus === "scanning" ? "Waiting for finger..." :
                       fpStatus === "processing" ? "Processing template..." :
                       "Status: " + fpStatus}
                    </div>
                  ) : (
                    <div className="flex gap-2">
                      <button onClick={()=>{setFpAction(null);setFpError(null);}} className="flex-1 py-1.5 text-xs rounded border border-border font-semibold hover:bg-muted">Cancel</button>
                      <button onClick={handleFpConfirm} className={`flex-1 py-1.5 text-xs rounded font-bold text-white ${fpAction==="remove"?"bg-red-600":"bg-sky-600"}`}>
                        {fpAction==="register"?"Confirm Enrollment":fpAction==="update"?"Confirm Update":"Confirm Removal"}
                      </button>
                    </div>
                  )}
                </div>
              )}
              <div className="bg-muted rounded p-3">
                <p className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-1">Note</p>
                <p className="text-xs text-muted-foreground">All fingerprint enrollment must be done here. Fingerprint enrollment is not available on the Attendance page.</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Client Form Modal (tabbed CRUD) ────────────────────────────────────────────
const BLANK: Omit<Client,"clientId"|"photoInitial"> = {
  docketNumber:"",fullName:"",middleInitial:"",gender:"Male",clientCategory:"Probationer",
  ccNumber:"",court:"",doNdo:"DO",assignedOfficer:"",supervisionStart:"",supervisionEnd:"",
  supervisionPhase:"Phase 1",registrationDate:todayStr,status:"Active",caseType:"Non-Drug",remarks:"",
  finalReport:"",finalReportDate:"",terminationDate:"",violationReport:"",violationDate:"",
  courtOrderDisposition:"",courtOrderDateSubmitted:"",courtOrderDateReceived:"",
  fingerprintId:"",fingerprintEnrolled:false,fingerprintEnrollmentDate:"",fingerprintImage:"",
  piNumber:"",alias:"",identifyingMarks:"",address:"",barangay:"",contactNumber:"",
  dateOfBirth:"",placeOfBirth:"",civilStatus:"Single",spouseName:"",numberOfDependents:"0",
  educationalAttainment:"",occupation:"",monthlyIncome:"",hobbies:"",skills:"",religiousAffiliation:"",
  psiNumber:"",criminalCaseNumber:"",chargedWith:"",dateCommitted:"",convictedOf:"",dateConvicted:"",
  sentence:"",placeOfReferral:"",datePsiSubmitted:"",custodyStatus:"",dateProbationGranted:"",
  dateProbationOrderReceived:"",periodOfProbation:"",dateFrSubmitted:"",dateOfToro:"",
  dateReceivedCase:"",investigatingOfficer:"",
};

function ClientFormModal({client,onSave,onClose,nextId}:{client?:Client;onSave:(c:Client)=>void;onClose:()=>void;nextId:string}) {
  type FormTab = "basic"|"personal"|"case"|"remarks";
  const [ftab,setFtab] = useState<FormTab>("basic");
  const [form,setForm] = useState<Omit<Client,"clientId"|"photoInitial">>(
    client ? {...BLANK,...Object.fromEntries(Object.entries(client).filter(([k])=>k!=="clientId"&&k!=="photoInitial"))} : BLANK
  );
  const set = (k: keyof typeof form, v: string|boolean) => setForm(f=>({...f,[k]:v}));

  const ftabs: {id:FormTab;label:string}[] = [{id:"basic",label:"Basic Info"},{id:"personal",label:"Personal"},{id:"case",label:"Case"},{id:"remarks",label:"Remarks"}];

  const inp = (label:string, k: keyof typeof form, opts?:{type?:string;placeholder?:string;required?:boolean;mono?:boolean}) => (
    <div key={k}>
      <label className="block text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-1">{label}{opts?.required?" *":""}</label>
      <input type={opts?.type??"text"} required={opts?.required} placeholder={opts?.placeholder}
        value={form[k] as string} onChange={e=>set(k,e.target.value)}
        className={`w-full px-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none focus:ring-2 focus:ring-sky-500/20 ${opts?.mono?"font-mono":""}`} />
    </div>
  );
  const sel = (label:string, k: keyof typeof form, opts:string[]) => (
    <div key={k}>
      <label className="block text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-1">{label}</label>
      <select value={form[k] as string} onChange={e=>set(k,e.target.value)}
        className="w-full px-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none">
        {opts.map(o=><option key={o}>{o}</option>)}
      </select>
    </div>
  );

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-3" onClick={onClose}>
      <div className="bg-card rounded-lg border border-border w-full max-w-xl max-h-[94vh] overflow-hidden flex flex-col" onClick={e=>e.stopPropagation()}>
        <div className="px-6 py-4 border-b border-border flex items-center justify-between flex-shrink-0">
          <h2 className="text-sm font-bold text-foreground">{client?"Edit Client Record":"New Client Registration"} — {client?.clientId??nextId}</h2>
          <button onClick={onClose}><X className="w-4 h-4 text-muted-foreground" /></button>
        </div>
        <div className="flex border-b border-border flex-shrink-0">
          {ftabs.map(({id,label})=>(
            <button key={id} onClick={()=>setFtab(id)}
              className={`flex-1 py-2.5 text-[11px] font-semibold transition-colors border-b-2 ${ftab===id?"border-sky-500 text-sky-600":"border-transparent text-muted-foreground hover:text-foreground"}`}>
              {label}
            </button>
          ))}
        </div>
        <form onSubmit={e=>{e.preventDefault();onSave({...form,clientId:client?.clientId??nextId,photoInitial:form.fullName.charAt(0).toUpperCase()||"?"});}} className="flex-1 overflow-y-auto">
          <div className="p-5 space-y-4">
            {ftab==="basic" && <>
              <div className="grid grid-cols-3 gap-3">
                <div className="col-span-2">{inp("Full Name (First Last)","fullName",{required:true,placeholder:"e.g. Maria Santos"})}</div>
                {inp("M.I.","middleInitial",{placeholder:"T."})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {sel("Gender","gender",["Male","Female"])}
                {sel("Category","clientCategory",["Probationer","Parolee","Pardonee"])}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Docket Number *","docketNumber",{required:true,placeholder:"DOC-YYYY-NNN",mono:true})}
                {inp("CC Number","ccNumber",{placeholder:"CC-YYYY-NNN",mono:true})}
              </div>
              {inp("Court","court",{placeholder:"RTC Branch 18, Tagaytay City"})}
              <div className="grid grid-cols-2 gap-3">
                {sel("DO / NDO","doNdo",["DO","NDO"])}
                {sel("Case Type","caseType",["Non-Drug","Drug"])}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Supervision Start *","supervisionStart",{required:true,type:"date"})}
                {inp("Supervision End *","supervisionEnd",{required:true,type:"date"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {sel("Phase","supervisionPhase",SUPERVISION_PHASES)}
                {sel("Status","status",["Active","Completed","Terminated","Inactive"])}
              </div>
              {sel("Supervising Officer","assignedOfficer",[""].concat(OFFICERS))}
            </>}

            {ftab==="personal" && <>
              <div className="grid grid-cols-2 gap-3">
                {inp("PI Number","piNumber",{mono:true})}{inp("Alias","alias")}
              </div>
              {inp("Identifying Marks","identifyingMarks")}
              {inp("Address","address",{placeholder:"Street, Barangay, City"})}
              <div className="grid grid-cols-2 gap-3">
                {inp("Barangay","barangay")}{inp("Contact Number","contactNumber",{mono:true,placeholder:"09XX XXX XXXX"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Date of Birth","dateOfBirth",{type:"date"})}{inp("Place of Birth","placeOfBirth")}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {sel("Civil Status","civilStatus",CIVIL_STATUSES)}{inp("Spouse's Name","spouseName")}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("No. of Dependents","numberOfDependents",{type:"number"})}{sel("Education","educationalAttainment",[""].concat(EDUCATION_LEVELS))}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Occupation","occupation")}{inp("Monthly Income","monthlyIncome",{placeholder:"₱ amount"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Hobbies","hobbies")}{inp("Skills","skills")}
              </div>
              {inp("Religious Affiliation","religiousAffiliation")}
            </>}

            {ftab==="case" && <>
              <div className="grid grid-cols-2 gap-3">
                {inp("PSI / PRPD Number","psiNumber",{mono:true})}{inp("Criminal Case No.","criminalCaseNumber",{mono:true})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Charged With","chargedWith")}{inp("Date Committed","dateCommitted",{type:"date"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Convicted Of","convictedOf")}{inp("Date Convicted","dateConvicted",{type:"date"})}
              </div>
              {inp("Sentence","sentence",{placeholder:"e.g. 3 years probation"})}
              <div className="grid grid-cols-2 gap-3">
                {inp("Place of Referral","placeOfReferral")}{inp("Date PSI Submitted","datePsiSubmitted",{type:"date"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Custody Status","custodyStatus")}{inp("Date Probation Granted","dateProbationGranted",{type:"date"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Date Order Received","dateProbationOrderReceived",{type:"date"})}{inp("Period of Probation","periodOfProbation",{placeholder:"e.g. 5 years"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Date FR/SR/RR/TR Submitted","dateFrSubmitted",{type:"date"})}{inp("Date of TORO","dateOfToro",{type:"date"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Date Received","dateReceivedCase",{type:"date"})}{sel("Investigating Officer","investigatingOfficer",[""].concat(OFFICERS))}
              </div>
            </>}

            {ftab==="remarks" && <>
              <div>
                <label className="block text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-1">General Remarks</label>
                <textarea value={form.remarks} onChange={e=>set("remarks",e.target.value)} rows={2} placeholder="General notes…"
                  className="w-full px-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none resize-none" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Final Report","finalReport",{placeholder:"Report details"})}{inp("Final Report Date","finalReportDate",{type:"date"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Violation Report","violationReport")}{inp("Violation Date","violationDate",{type:"date"})}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Termination Date","terminationDate",{type:"date"})}{inp("Court Order Disposition","courtOrderDisposition")}
              </div>
              <div className="grid grid-cols-2 gap-3">
                {inp("Date Submitted","courtOrderDateSubmitted",{type:"date"})}{inp("Date Received","courtOrderDateReceived",{type:"date"})}
              </div>
            </>}
          </div>
          <div className="px-5 pb-5 flex gap-2 sticky bottom-0 bg-card border-t border-border pt-4">
            <button type="button" onClick={onClose} className="flex-1 py-2.5 rounded border border-border text-xs font-semibold text-foreground hover:bg-muted">Cancel</button>
            <button type="submit" className="flex-1 py-2.5 rounded text-xs font-bold text-white hover:opacity-90" style={{background:"var(--primary)"}}>
              {client?"Save Changes":"Register Client"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// LOGIN
// ══════════════════════════════════════════════════════════════════════════════
function LoginPage({onLogin}:{onLogin:(user:any)=>void}) {
  const [user,setUser] = useState("");
  const [pass,setPass] = useState("");
  const [err,setErr] = useState("");
  return (
    <div className="min-h-screen flex bg-background">
      <div className="hidden lg:flex flex-col justify-between w-[42%] p-12" style={{background:"var(--primary)"}}>
        <div>
          <div className="flex items-center gap-3 mb-8">
            <div className="w-12 h-12 rounded-xl bg-sky-500 flex items-center justify-center"><Shield className="w-6 h-6 text-white" /></div>
            <div>
              <div className="text-white font-bold text-sm leading-tight">Republic of the Philippines</div>
              <div className="text-sky-300 text-xs">Department of Justice — Parole and Probation Administration</div>
            </div>
          </div>
          <div className="w-12 h-px bg-sky-700 mb-7" />
          <h2 className="text-3xl font-bold text-white leading-snug mb-3">Tagaytay Parole<br />&amp; Probation Office</h2>
          <p className="text-sky-300 text-sm leading-relaxed mb-7">Automated attendance monitoring for Probationers, Parolees, and Pardonees using biometric fingerprint integration.</p>
          <div className="space-y-2.5">
            {["Futronic FS64 fingerprint verification","Monthly compliance matrix with supervision history","PPA Form 12 personal &amp; case profiles","Reporting by officer, category, and gender"].map((t,i)=>(
              <div key={i} className="flex items-center gap-2.5">
                <Check className="w-3.5 h-3.5 text-sky-400 flex-shrink-0" />
                <span className="text-sky-100 text-xs" dangerouslySetInnerHTML={{__html:t}} />
              </div>
            ))}
          </div>
        </div>
        <p className="text-sky-700 text-[10px] font-mono">AMS v3.0 — Tagaytay PPO © 2026</p>
      </div>
      <div className="flex-1 flex items-center justify-center p-8">
        <div className="w-full max-w-xs">
          <div className="mb-7">
            <div className="lg:hidden flex items-center gap-2 mb-5"><Shield className="w-5 h-5 text-sky-600" /><span className="font-bold text-foreground text-sm">Tagaytay PPO — AMS</span></div>
            <h1 className="text-xl font-bold text-foreground">Sign In</h1>
            <p className="text-xs text-muted-foreground mt-1">Authorized personnel only</p>
          </div>
          <form onSubmit={async e=>{
            e.preventDefault();
            if(!user||!pass){setErr("Enter credentials.");return;}
            try {
              const res = await fetch(import.meta.env.BASE_URL + 'api/login.php', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({username: user, password: pass})
              });
              const data = await res.json();
              if (data.success) {
                onLogin(data.user);
              } else {
                setErr(data.message || "Login failed.");
              }
            } catch (err) {
              setErr("Network error.");
            }
          }} className="space-y-4">
            <div>
              <label className="block text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-1.5">Username</label>
              <input value={user} onChange={e=>{setUser(e.target.value);setErr("");}}
                className="w-full px-3 py-2.5 rounded border border-border bg-input-background text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-sky-500/30 font-mono" placeholder="Enter username" />
            </div>
            <div>
              <label className="block text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-1.5">Password</label>
              <input type="password" value={pass} onChange={e=>{setPass(e.target.value);setErr("");}}
                className="w-full px-3 py-2.5 rounded border border-border bg-input-background text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-sky-500/30" placeholder="••••••••" />
            </div>
            {err && <div className="flex items-center gap-2 text-xs text-red-600 bg-red-50 border border-red-200 rounded px-3 py-2"><AlertCircle className="w-3.5 h-3.5" />{err}</div>}
            <button type="submit" className="w-full py-2.5 rounded text-sm font-bold text-white hover:opacity-90" style={{background:"var(--primary)"}}>Sign In</button>
          </form>
          <p className="text-center text-[10px] text-muted-foreground mt-5 font-mono">Demo: any username + password</p>
        </div>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// DASHBOARD
// ══════════════════════════════════════════════════════════════════════════════
// Cutoff date presets for Case Summary
const CUTOFF_PRESETS = [
  {label:"End of March",    date:`${YEAR}-03-31`},
  {label:"End of June",     date:`${YEAR}-06-30`},
  {label:"End of September",date:`${YEAR}-09-30`},
  {label:"End of December", date:`${YEAR}-12-31`},
];

function DashboardPage({clients,recs,setPage}:{clients:Client[];recs:AttendanceRecord[];setPage:(p:Page)=>void}) {
  const active = clients.filter(c=>c.status==="Active");
  const curPrefix = `${YEAR}-${String(CUR_M+1).padStart(2,"0")}`;
  const attendedIds = new Set(recs.filter(r=>r.date.startsWith(curPrefix)).map(r=>r.clientId));
  const monthlyAttended = active.filter(c=>attendedIds.has(c.clientId)).length;
  const pending = active.filter(c=>!attendedIds.has(c.clientId) && c.supervisionEnd >= todayStr);
  const missingRemarks = active.filter(c=>!c.remarks&&!c.finalReport&&!c.violationReport);
  const ended = active.filter(c=>supStatus(c)==="Ended");
  const nearExpiry = active.filter(c=>supStatus(c)==="Near Expiry");
  const recent = [...recs].sort((a,b)=>b.date.localeCompare(a.date)||b.time.localeCompare(a.time)).slice(0,5);

  // Counts by officer/category/gender
  const byOfficer = OFFICERS.map(o=>({officer:o,count:active.filter(c=>c.assignedOfficer===o).length}));
  const byCat = CLIENT_CATEGORIES.map(cat=>({cat,count:active.filter(c=>c.clientCategory===cat).length}));
  const males = active.filter(c=>c.gender==="Male").length;
  const females = active.filter(c=>c.gender==="Female").length;
  const compliancePct = active.length>0 ? Math.round((monthlyAttended/active.length)*100) : 0;

  const pieData=[{name:"Attended",value:monthlyAttended,color:"#16a34a"},{name:"Pending",value:Math.max(0,pending.length),color:"#e4eaf2"}];

  // Case Summary state
  const [cutoffDate, setCutoffDate] = useState(CUTOFF_PRESETS[1].date); // default: End of June
  const cutoffClients = clients.filter(c=>
    c.status==="Active" &&
    c.supervisionStart <= cutoffDate &&
    c.supervisionEnd >= cutoffDate
  );
  const drugCount = cutoffClients.filter(c=>c.caseType==="Drug").length;
  const nonDrugCount = cutoffClients.filter(c=>c.caseType==="Non-Drug").length;
  const totalCount = cutoffClients.length;
  const drugPct = totalCount>0 ? Math.round((drugCount/totalCount)*100) : 0;
  const nonDrugPct = totalCount>0 ? Math.round((nonDrugCount/totalCount)*100) : 0;
  const casePieData = [
    {name:"Drug Cases",    value:drugCount,    color:"#dc2626"},
    {name:"Non-Drug Cases",value:nonDrugCount, color:"#0284c7"},
  ];

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      <Topbar title="Dashboard" sub="Tagaytay PPO — Monthly Overview" />
      <div className="flex-1 overflow-y-auto p-5 space-y-5">

        {/* Summary bar */}
        <div className="bg-card rounded border border-border p-4 space-y-3">
          {/* Headline count — prominent */}
          <div className="flex items-center justify-between">
            <div className="flex items-baseline gap-3">
              <span className="text-3xl font-bold text-foreground">{active.length}</span>
              <span className="text-sm font-semibold text-muted-foreground">Active Clients</span>
              <span className="text-lg font-bold text-blue-600">{males}M</span>
              <span className="text-sm text-muted-foreground">/</span>
              <span className="text-lg font-bold text-pink-600">{females}F</span>
            </div>
            <div className="text-right">
              <div className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">By Category</div>
              <div className="flex items-center gap-2">
                {byCat.map(({cat,count})=>(
                  <div key={cat} className="flex items-center gap-1">
                    <Badge status={cat} />
                    <span className="text-sm font-bold text-foreground">{count}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
          {/* Per-officer breakdown */}
          <div className="flex flex-wrap items-center gap-4 pt-2 border-t border-border">
            <span className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground">By Officer:</span>
            {byOfficer.map(({officer,count})=>(
              <div key={officer} className="flex items-center gap-1.5">
                <span className="text-xs font-semibold text-foreground">{officer}</span>
                <span className="text-base font-bold" style={{color:"var(--primary)"}}>{count}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Case Summary */}
        <div className="bg-card rounded border border-border p-5">
          <div className="flex flex-wrap items-center justify-between gap-4 mb-4">
            <div>
              <h3 className="text-sm font-bold text-foreground">Case Summary</h3>
              <p className="text-[11px] text-muted-foreground font-mono">Active caseload as of selected reporting period</p>
            </div>
            {/* Cutoff date selector */}
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Cutoff:</span>
              {CUTOFF_PRESETS.map(p=>(
                <button key={p.date} onClick={()=>setCutoffDate(p.date)}
                  className={`px-3 py-1.5 rounded-md text-[11px] font-semibold transition-all border-2 ${cutoffDate===p.date?"border-sky-500 bg-sky-500 text-white":"border-border bg-input-background text-muted-foreground hover:border-sky-300 hover:text-foreground"}`}>
                  {p.label}
                </button>
              ))}
              <input type="date" value={cutoffDate} onChange={e=>setCutoffDate(e.target.value)}
                className="px-2.5 py-1.5 text-[11px] rounded border-2 border-border bg-input-background text-foreground focus:outline-none focus:border-sky-400 font-mono" />
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-4 gap-4 items-center">
            {/* Donut chart */}
            <div className="flex flex-col items-center">
              <ResponsiveContainer width="100%" height={140}>
                <PieChart>
                  <Pie data={casePieData.filter(d=>d.value>0).length>0?casePieData:[{name:"No Data",value:1,color:"#e4eaf2"}]}
                    cx="50%" cy="50%" innerRadius={42} outerRadius={62} paddingAngle={2} dataKey="value">
                    {casePieData.map((e,i)=><Cell key={i} fill={e.color} />)}
                  </Pie>
                  <Tooltip formatter={(v:number,n:string)=>[v,n]} contentStyle={{fontSize:11,borderRadius:6}} />
                </PieChart>
              </ResponsiveContainer>
              <p className="text-[10px] text-muted-foreground font-mono text-center -mt-1">
                as of <strong className="text-foreground">{cutoffDate}</strong>
              </p>
            </div>

            {/* Summary cards */}
            <div className="lg:col-span-3 grid grid-cols-3 gap-3">
              {/* Total */}
              <div className="bg-muted rounded-lg p-4 flex flex-col items-center text-center border border-border">
                <span className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground mb-1">Total Cases</span>
                <span className="text-4xl font-bold text-foreground">{totalCount}</span>
                <span className="text-xs text-muted-foreground mt-1">Active clients</span>
              </div>
              {/* Drug */}
              <div className="bg-red-50 rounded-lg p-4 flex flex-col items-center text-center border border-red-200">
                <span className="text-[10px] font-mono uppercase tracking-widest text-red-700 mb-1">Drug Cases</span>
                <span className="text-4xl font-bold text-red-700">{drugCount}</span>
                <div className="w-full mt-2">
                  <div className="w-full h-2 bg-red-100 rounded-full overflow-hidden">
                    <div className="h-2 bg-red-500 rounded-full transition-all" style={{width:`${drugPct}%`}} />
                  </div>
                  <span className="text-xs font-bold text-red-600 mt-1 block">{drugPct}%</span>
                </div>
              </div>
              {/* Non-Drug */}
              <div className="bg-sky-50 rounded-lg p-4 flex flex-col items-center text-center border border-sky-200">
                <span className="text-[10px] font-mono uppercase tracking-widest text-sky-700 mb-1">Non-Drug Cases</span>
                <span className="text-4xl font-bold text-sky-700">{nonDrugCount}</span>
                <div className="w-full mt-2">
                  <div className="w-full h-2 bg-sky-100 rounded-full overflow-hidden">
                    <div className="h-2 bg-sky-500 rounded-full transition-all" style={{width:`${nonDrugPct}%`}} />
                  </div>
                  <span className="text-xs font-bold text-sky-600 mt-1 block">{nonDrugPct}%</span>
                </div>
              </div>
            </div>
          </div>

          {/* Per-officer drug breakdown */}
          <div className="mt-4 pt-4 border-t border-border grid grid-cols-3 gap-3">
            {OFFICERS.map(officer=>{
              const oc = cutoffClients.filter(c=>c.assignedOfficer===officer);
              const od = oc.filter(c=>c.caseType==="Drug").length;
              const on = oc.filter(c=>c.caseType==="Non-Drug").length;
              return (
                <div key={officer} className="bg-muted/50 rounded p-3">
                  <p className="text-[10px] font-semibold text-foreground mb-1.5 truncate">{officer}</p>
                  <div className="flex items-center gap-2 text-xs">
                    <span className="text-base font-bold text-foreground">{oc.length}</span>
                    <span className="text-muted-foreground">total</span>
                    <span className="w-px h-3 bg-border" />
                    <span className="font-bold text-red-600">{od}D</span>
                    <span className="font-bold text-sky-600">{on}ND</span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Supervision status alerts */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
          <div className={`rounded border p-4 flex flex-col ${ended.length>0?"bg-red-50 border-red-200":"bg-muted border-border opacity-50"}`} style={{minHeight:"120px",maxHeight:"160px"}}>
            <div className="flex items-center gap-2 mb-2 flex-shrink-0">
              <div className="w-2.5 h-2.5 rounded-full bg-red-500 flex-shrink-0" />
              <h3 className="text-xs font-bold text-red-800">Ended Supervision</h3>
              <span className="ml-auto text-xs font-bold text-red-700 bg-red-100 border border-red-200 px-2 py-0.5 rounded-full">{ended.length}</span>
            </div>
            <div className="overflow-y-auto flex-1 space-y-1 pr-1">
              {ended.length===0 && <p className="text-[11px] text-muted-foreground italic">None</p>}
              {ended.map(c=>(
                <div key={c.clientId} className="text-[11px] text-red-700 flex justify-between gap-2">
                  <span className="truncate font-medium">{formatName(c)}</span>
                  <span className="font-mono flex-shrink-0">{c.supervisionEnd}</span>
                </div>
              ))}
            </div>
          </div>
          <div className={`rounded border p-4 flex flex-col ${nearExpiry.length>0?"bg-amber-50 border-amber-200":"bg-muted border-border opacity-50"}`} style={{minHeight:"120px",maxHeight:"160px"}}>
            <div className="flex items-center gap-2 mb-2 flex-shrink-0">
              <div className="w-2.5 h-2.5 rounded-full bg-amber-400 flex-shrink-0" />
              <h3 className="text-xs font-bold text-amber-800">Ending Within 30 Days</h3>
              <span className="ml-auto text-xs font-bold text-amber-700 bg-amber-100 border border-amber-200 px-2 py-0.5 rounded-full">{nearExpiry.length}</span>
            </div>
            <div className="overflow-y-auto flex-1 space-y-1 pr-1">
              {nearExpiry.length===0 && <p className="text-[11px] text-muted-foreground italic">None</p>}
              {nearExpiry.map(c=>(
                <div key={c.clientId} className="text-[11px] text-amber-700 flex justify-between gap-2">
                  <span className="truncate font-medium">{formatName(c)}</span>
                  <span className="font-mono flex-shrink-0">{c.supervisionEnd}</span>
                </div>
              ))}
            </div>
          </div>
          <div className={`rounded border p-4 flex flex-col ${missingRemarks.length>0?"bg-violet-50 border-violet-200":"bg-muted border-border opacity-50"}`} style={{minHeight:"120px",maxHeight:"160px"}}>
            <div className="flex items-center gap-2 mb-2 flex-shrink-0">
              <div className="w-2.5 h-2.5 rounded-full bg-violet-400 flex-shrink-0" />
              <h3 className="text-xs font-bold text-violet-800">Missing Remarks</h3>
              <span className="ml-auto text-xs font-bold text-violet-700 bg-violet-100 border border-violet-200 px-2 py-0.5 rounded-full">{missingRemarks.length}</span>
            </div>
            <div className="overflow-y-auto flex-1 space-y-1 pr-1">
              {missingRemarks.length===0 && <p className="text-[11px] text-muted-foreground italic">None — all clients have remarks ✓</p>}
              {missingRemarks.map(c=>(
                <div key={c.clientId} className="text-[11px] text-violet-700 font-medium truncate">{formatName(c)}</div>
              ))}
            </div>
          </div>
        </div>

        {/* Stats row */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
          {[
            {l:"Current Total Clients",v:active.length,sub:`${males}M / ${females}F`,icon:Users,c:"bg-sky-100 text-sky-700"},
            {l:"Monthly Attendance",v:monthlyAttended,sub:`${MONTHS[CUR_M]} ${YEAR}`,icon:CheckCircle2,c:"bg-emerald-100 text-emerald-700"},
            {l:"Pending Attendance",v:pending.length,sub:"Not yet reported this month",icon:Clock,c:"bg-amber-100 text-amber-700"},
            {l:"Compliance Rate",v:`${compliancePct}%`,sub:`${ended.length} ended, ${nearExpiry.length} ending soon`,icon:Shield,c:"bg-violet-100 text-violet-700"},
          ].map(({l,v,sub,icon:Icon,c})=>(
            <div key={l} className="bg-card rounded border border-border p-4 flex items-start gap-3">
              <div className={`w-9 h-9 rounded flex items-center justify-center flex-shrink-0 ${c}`}><Icon className="w-4 h-4" /></div>
              <div>
                <div className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground">{l}</div>
                <div className="text-2xl font-bold text-foreground leading-tight">{v}</div>
                <div className="text-[10px] text-muted-foreground mt-0.5">{sub}</div>
              </div>
            </div>
          ))}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
          {/* Did not report panel */}
          <div className="bg-card rounded border border-border flex flex-col" style={{maxHeight:"260px"}}>
            <div className="px-5 py-3 border-b border-border flex items-center justify-between flex-shrink-0">
              <h3 className="text-sm font-bold text-foreground">Did Not Report</h3>
              <div className="flex items-center gap-2">
                <span className="text-[10px] text-muted-foreground font-mono">{MONTHS[CUR_M]} {YEAR}</span>
                <span className="text-sm font-bold text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full">{pending.length}</span>
              </div>
            </div>
            <div className="divide-y divide-border overflow-y-auto flex-1">
              {pending.map(c=>(
                <div key={c.clientId} className="px-5 py-2.5 flex items-center justify-between hover:bg-muted/20">
                  <div className="min-w-0">
                    <p className="text-xs font-semibold text-foreground truncate">{formatName(c)}</p>
                    <p className="text-[10px] font-mono text-muted-foreground">{c.docketNumber} · {c.assignedOfficer}</p>
                  </div>
                  <Badge status={c.clientCategory} />
                </div>
              ))}
              {pending.length===0 && <div className="px-5 py-6 text-center text-[11px] text-emerald-600 font-semibold">All active clients reported this month ✓</div>}
            </div>
          </div>

          {/* Monthly chart */}
          <div className="lg:col-span-2 bg-card rounded border border-border p-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-sm font-bold text-foreground">Monthly Attendance — {YEAR}</h3>
                <p className="text-[11px] text-muted-foreground font-mono">Jan – Jun actual reporting</p>
              </div>
              <button onClick={()=>setPage("reports")} className="text-[11px] text-sky-600 hover:underline font-semibold">Full Report →</button>
            </div>
            <ResponsiveContainer width="100%" height={160}>
              <BarChart data={getMonthlyChartData(clients, recs, YEAR).slice(0, CUR_M + 1)} barCategoryGap="35%">
                <CartesianGrid strokeDasharray="3 3" stroke="#e4eaf2" />
                <XAxis dataKey="month" tick={{fontSize:11,fill:"#5d7290",fontFamily:"JetBrains Mono"}} axisLine={false} tickLine={false} />
                <YAxis tick={{fontSize:11,fill:"#5d7290",fontFamily:"JetBrains Mono"}} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={{fontSize:12,borderRadius:6,border:"1px solid #e4eaf2"}} />
                <Bar dataKey="attended" name="Attended" fill="#16a34a" radius={[3,3,0,0]} />
                <Bar dataKey="total" name="Total Active" fill="#dce8f5" radius={[3,3,0,0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Recent logs */}
        <div className="bg-card rounded border border-border">
          <div className="px-5 py-3 border-b border-border flex items-center justify-between">
            <h3 className="text-sm font-bold text-foreground">Recent Attendance Logs</h3>
            <button onClick={()=>setPage("history")} className="text-[11px] text-sky-600 hover:underline font-semibold">View Matrix →</button>
          </div>
          <div className="divide-y divide-border">
            {recent.map(r=>{
              const c=clients.find(x=>x.clientId===r.clientId);
              return (
                <div key={r.attendanceId} className="px-5 py-2.5 flex items-center gap-4 hover:bg-muted/30">
                  <div className="flex-1 min-w-0">
                    <span className="text-xs font-semibold text-foreground">{c?formatName(c):r.fullName}</span>
                    <span className="text-[10px] text-muted-foreground font-mono ml-2">{r.docketNumber}</span>
                  </div>
                  <Fingerprint className="w-3.5 h-3.5 text-sky-400 flex-shrink-0" />
                  <span className="text-[11px] font-mono text-muted-foreground">{r.date} {r.time}</span>
                  <Badge status="Present" />
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// ATTENDANCE
// ══════════════════════════════════════════════════════════════════════════════
function AttendancePage({clients,recs,addRec,updateRec}:{clients:Client[];recs:AttendanceRecord[];addRec:(r:AttendanceRecord)=>void;updateRec:(id:string,status:string)=>void}) {
  type ScanState="idle"|"connecting"|"scanning"|"confirm"|"recorded"|"duplicate"|"notfound"|"disconnected";
  const [state,setState] = useState<ScanState>("idle");
  const [matched,setMatched] = useState<Client|null>(null);
  const [scanError,setScanError] = useState("");
  const [pendingTime,setPendingTime] = useState("");
  const [scanIdx,setScanIdx] = useState(0);
  const [showManual,setShowManual] = useState(false);
  const [manualQ,setManualQ] = useState("");
  const [manualClient,setManualClient] = useState<Client|null>(null);
  const [manualDate,setManualDate] = useState(todayStr);
  const [manualDone,setManualDone] = useState(false);
  const weekDates = pastWeekDates();
  const [viewDate,setViewDate] = useState(todayStr);
  const [infoClient,setInfoClient] = useState<Client|null>(null);

  const ws = useRef<WebSocket | null>(null);
  const clientsRef = useRef(clients);
  const recsRef = useRef(recs);
  useEffect(() => { clientsRef.current = clients; }, [clients]);
  useEffect(() => { recsRef.current = recs; }, [recs]);

  useEffect(() => {
    const connect = () => {
      ws.current = new WebSocket("ws://localhost:5000/");
      ws.current.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          if (data.status === "connecting") {
            setState("connecting");
          } else if (data.status === "scanner_ready") {
            // Hardware confirmed connected, will transition to scanning next
          } else if (data.status === "scanning") {
            setState("scanning");
          } else if (data.success && data.match) {
            const client = clientsRef.current.find(c => c.clientId === data.clientId);
            if (client) {
              const todayRecs = recsRef.current.filter(r=>r.date===todayStr);
              if (todayRecs.some(r=>r.clientId===client.clientId)) {
                setMatched(client);
                setState("duplicate");
              } else {
                const now=new Date();
                const t=`${String(now.getHours()).padStart(2,"0")}:${String(now.getMinutes()).padStart(2,"0")}`;
                setMatched(client); setPendingTime(t); setState("confirm");
              }
            } else {
              setState("notfound");
            }
          } else if (data.success && !data.match) {
            setState("notfound");
          } else if (data.success === false) {
            console.error("Scanner Error:", data.error);
            if (data.error_type === "scanner_not_connected") {
              setScanError(data.error || "Fingerprint scanner is not connected. Please plug in the Futronic FS64 and try again.");
              setState("disconnected");
            } else {
              setState("notfound");
            }
          }
        } catch (e) {
          console.error("WS parse error", e);
        }
      };
    };
    connect();
    return () => {
      if (ws.current) ws.current.close();
    };
  }, []);


  const todayRecs = recs.filter(r=>r.date===todayStr);
  const viewRecs = recs.filter(r=>r.date===viewDate);

  const doScan = () => {
    setState("connecting"); setMatched(null); setScanError("");
    if (ws.current && ws.current.readyState === WebSocket.OPEN) {
      ws.current.send(JSON.stringify({ action: "start_scan" }));
    } else {
      console.error("WebSocket not connected. Trying to reconnect...");
      ws.current = new WebSocket("ws://localhost:5000/");
      ws.current.onopen = () => {
        ws.current!.send(JSON.stringify({ action: "start_scan" }));
      };
      ws.current.onerror = () => {
        setScanError("Cannot connect to the fingerprint service. Make sure FPTester.exe is running.");
        setState("disconnected");
      };
      ws.current.onmessage = (event) => {
        // Re-dispatch to the main handler
        try {
          const data = JSON.parse(event.data);
          if (data.status === "connecting") setState("connecting");
          else if (data.status === "scanning") setState("scanning");
          else if (data.success === false && data.error_type === "scanner_not_connected") {
            setScanError(data.error || "Scanner not connected.");
            setState("disconnected");
          }
        } catch {}
      };
    }
  };

  const cancelScan = () => {
    setState("idle");
    if (ws.current && ws.current.readyState === WebSocket.OPEN) {
      ws.current.send(JSON.stringify({ action: "stop_scan" }));
    }
  };

  const confirmAttendance = () => {
    if (!matched) return;
    addRec({attendanceId:`ATT-NEW-${Date.now()}`,clientId:matched.clientId,fullName:matched.fullName,
      caseNumber:matched.ccNumber,docketNumber:matched.docketNumber,date:todayStr,time:pendingTime,verifiedBy:"Futronic FS64",status:"Present"});
    setState("recorded");
    alert("Attendance recorded successfully for " + matched.fullName);
  };

  const reset = () => { setState("idle"); setMatched(null); setPendingTime(""); };

  const manualResults = manualQ.length>1
    ? clients.filter(c=>c.status==="Active"&&[c.fullName,formatName(c)].some(v=>v.toLowerCase().includes(manualQ.toLowerCase()))).slice(0,6)
    : [];

  const markManual = () => {
    if (!manualClient) return;
    if (recs.some(r=>r.clientId===manualClient.clientId&&r.date===manualDate)) { setManualDone(false); return; }
    const now=new Date();
    const t=`${String(now.getHours()).padStart(2,"0")}:${String(now.getMinutes()).padStart(2,"0")}`;
    addRec({attendanceId:`ATT-MANUAL-${Date.now()}`,clientId:manualClient.clientId,fullName:manualClient.fullName,
      caseNumber:manualClient.ccNumber,docketNumber:manualClient.docketNumber,date:manualDate,time:t,verifiedBy:"Manual Entry",status:"Present"});
    setManualDone(true); setViewDate(manualDate);
    setTimeout(()=>{ setManualClient(null); setManualQ(""); setManualDone(false); },2000);
  };

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      <Topbar title="Attendance" sub="Right thumb fingerprint — Futronic FS64" />
      <div className="flex-1 overflow-y-auto p-5">
        <div className="grid grid-cols-1 lg:grid-cols-5 gap-5">
          {/* Scanner */}
          <div className="lg:col-span-2 space-y-4">
            <div className="bg-card rounded border border-border p-6 flex flex-col items-center text-center">
              <div className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground mb-1">Futronic FS64 — Right Thumb Only</div>
              <div onClick={state==="idle"?doScan:undefined}
                className="relative w-44 h-44 rounded-3xl flex items-center justify-center mb-5 cursor-pointer select-none transition-all border-2 mt-4"
                style={{background:state==="recorded"?"#f0fdf4":state==="duplicate"?"#fefce8":state==="notfound"?"#fef2f2":state==="disconnected"?"#fef2f2":state==="scanning"||state==="confirm"?"#f0f9ff":state==="connecting"?"#f0f9ff":"var(--input-background)",borderColor:state==="recorded"?"#16a34a":state==="duplicate"?"#ca8a04":state==="notfound"?"#dc2626":state==="disconnected"?"#dc2626":state==="scanning"||state==="confirm"?"var(--accent)":state==="connecting"?"var(--accent)":"var(--border)"}}>
                {(state==="scanning"||state==="confirm")&&<div className="absolute inset-0 rounded-3xl border-2 border-sky-400 animate-ping opacity-30" />}
                {state==="connecting"&&<div className="absolute inset-0 rounded-3xl border-2 border-sky-400 animate-pulse opacity-40" />}
                <Fingerprint className={`w-24 h-24 transition-colors ${state==="recorded"?"text-emerald-500":state==="duplicate"?"text-amber-500":state==="notfound"||state==="disconnected"?"text-red-400":state==="scanning"||state==="confirm"?"text-sky-500 animate-pulse":state==="connecting"?"text-sky-400 animate-pulse":"text-muted-foreground"}`} />
                {state==="scanning"&&<div className="absolute left-6 right-6 h-0.5 bg-sky-400 opacity-80 rounded-full" style={{animation:"scanline 1.5s ease-in-out infinite"}} />}
              </div>
              {state==="idle"&&<><p className="text-sm font-semibold text-foreground mb-1">Ready to Scan</p><p className="text-xs text-muted-foreground mb-5">Client places <strong>right thumb</strong> on scanner</p><button onClick={doScan} className="w-full py-2.5 rounded text-sm font-bold text-white hover:opacity-90" style={{background:"var(--primary)"}}>Activate Scanner</button></>}
              {state==="connecting"&&<><p className="text-sm font-bold text-sky-600 mb-1">Connecting to Scanner…</p><p className="text-xs text-muted-foreground mb-4">Checking if Futronic FS64 is connected</p><button onClick={cancelScan} className="w-full py-2.5 rounded text-sm font-bold bg-amber-500 text-white hover:opacity-90">Cancel</button></>}
              {state==="scanning"&&<><p className="text-sm font-bold text-sky-600 mb-1">Scanner Active — Waiting for Finger</p><p className="text-xs text-muted-foreground mb-4">Place right thumb on the scanner</p><button onClick={cancelScan} className="w-full py-2.5 rounded text-sm font-bold bg-amber-500 text-white hover:opacity-90">Cancel Scan</button></>}
              {state==="disconnected"&&<><div className="flex items-center gap-1.5 text-red-600 mb-2"><AlertCircle className="w-5 h-5" /><span className="text-sm font-bold">Scanner Not Connected</span></div><p className="text-xs text-muted-foreground mb-4">{scanError || "Please plug in the Futronic FS64 scanner and try again."}</p><button onClick={reset} className="w-full py-2 rounded text-xs font-bold border border-border text-foreground hover:bg-muted"><RefreshCw className="w-3.5 h-3.5 inline mr-1" />Try Again</button></>}
              {state==="confirm"&&<><p className="text-sm font-bold text-sky-600 mb-1">Match Found</p><p className="text-xs text-muted-foreground">Confirm attendance in the popup</p></>}
              {state==="recorded"&&matched&&<>
                <div className="flex items-center gap-1.5 text-emerald-600 mb-2"><BadgeCheck className="w-5 h-5" /><span className="text-sm font-bold">Attendance Recorded</span></div>
                <div className="w-full bg-emerald-50 border border-emerald-200 rounded p-3 mb-4 text-left">
                  <p className="text-sm font-bold text-foreground">{formatName(matched)}</p>
                  <p className="text-[10px] font-mono text-muted-foreground">{matched.docketNumber}</p>
                  <div className="grid grid-cols-2 gap-1 text-[10px] font-mono border-t border-emerald-200 pt-2 mt-2">
                    <span className="text-muted-foreground">Status:</span><span className="text-emerald-700 font-bold">PRESENT</span>
                    <span className="text-muted-foreground">Time:</span><span>{pendingTime}</span>
                  </div>
                </div>
                <div className="flex gap-2 w-full">
                  <button onClick={()=>setInfoClient(matched)} className="flex-1 py-2 rounded text-xs font-semibold border border-border text-foreground hover:bg-muted"><Eye className="w-3.5 h-3.5 inline mr-1" />View Profile</button>
                  <button onClick={reset} className="flex-1 py-2 rounded text-xs font-bold text-white hover:opacity-90" style={{background:"var(--primary)"}}><RefreshCw className="w-3.5 h-3.5 inline mr-1" />Next</button>
                </div>
              </>}
              {state==="duplicate"&&matched&&<>
                <div className="flex items-center gap-1.5 text-amber-600 mb-2"><AlertTriangle className="w-5 h-5" /><span className="text-sm font-bold">Already Recorded</span></div>
                <div className="w-full bg-amber-50 border border-amber-200 rounded p-3 mb-4 text-left">
                  <p className="text-xs font-semibold">{formatName(matched)}</p>
                  <p className="text-[10px] text-amber-700 mt-1">Attendance already recorded today.</p>
                </div>
                <button onClick={reset} className="w-full py-2 rounded text-xs font-bold border border-border text-foreground hover:bg-muted"><RefreshCw className="w-3.5 h-3.5 inline mr-1" />Scan Another</button>
              </>}
              {state==="notfound"&&<>
                <div className="flex items-center gap-1.5 text-red-600 mb-2"><XCircle className="w-5 h-5" /><span className="text-sm font-bold">Not Recognized</span></div>
                <p className="text-xs text-muted-foreground mb-4">Fingerprint not found. Contact your probation officer.</p>
                <button onClick={reset} className="w-full py-2 rounded text-xs font-bold border border-border text-foreground hover:bg-muted"><RefreshCw className="w-3.5 h-3.5 inline mr-1" />Try Again</button>
              </>}
            </div>
            <div className="bg-card rounded border border-border p-4 space-y-2">
              <p className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground">Today — {todayStr}</p>
              <div className="flex justify-between text-xs"><span className="flex items-center gap-2"><span className="w-2 h-2 rounded-full bg-emerald-500" />Attended</span><span className="font-mono font-bold">{todayRecs.length}</span></div>
              <div className="flex justify-between text-xs"><span className="flex items-center gap-2"><span className="w-2 h-2 rounded-full bg-amber-400" />Not Yet</span><span className="font-mono font-bold">{clients.filter(c=>c.status==="Active").length-todayRecs.length}</span></div>
            </div>
            {/* Manual fallback */}
            <div className="bg-card rounded border border-border overflow-hidden">
              <button onClick={()=>setShowManual(v=>!v)} className="w-full px-5 py-3 flex items-center justify-between text-xs font-semibold text-muted-foreground hover:bg-muted/30 transition-colors">
                <span className="flex items-center gap-2"><Search className="w-3.5 h-3.5" />Manual Entry (Scanner Fallback)</span>
                {showManual?<ChevronUp className="w-4 h-4" />:<ChevronDown className="w-4 h-4" />}
              </button>
              {showManual&&(
                <div className="px-5 pb-5 space-y-3 border-t border-border pt-4">
                  <p className="text-[10px] text-muted-foreground">Search existing clients only. Attendance recorded the same as fingerprint attendance.</p>
                  <div className="relative">
                    <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                    <input value={manualQ} onChange={e=>{setManualQ(e.target.value);setManualClient(null);setManualDone(false);}}
                      placeholder="Search by client name…"
                      className="w-full pl-8 pr-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none font-mono" />
                  </div>
                  {manualResults.length>0&&!manualClient&&(
                    <div className="border border-border rounded overflow-hidden">
                      {manualResults.map(c=>(
                        <button key={c.clientId} onClick={()=>{setManualClient(c);setManualQ(formatName(c));}}
                          className="w-full px-3 py-2.5 text-left hover:bg-muted/40 border-b border-border last:border-b-0">
                          <p className="text-xs font-semibold text-foreground">{formatName(c)}</p>
                          <p className="text-[10px] font-mono text-muted-foreground">{c.docketNumber} · {c.clientCategory}</p>
                        </button>
                      ))}
                    </div>
                  )}
                  {manualClient&&(
                    <div className="bg-sky-50 border border-sky-200 rounded p-3">
                      <div className="flex items-center justify-between mb-2">
                        <div>
                          <p className="text-xs font-bold text-foreground">{formatName(manualClient)}</p>
                          <p className="text-[10px] font-mono text-muted-foreground">{manualClient.docketNumber} · {manualClient.clientCategory}</p>
                        </div>
                        <button onClick={()=>{setManualClient(null);setManualQ("");}} className="text-muted-foreground hover:text-foreground"><X className="w-3.5 h-3.5" /></button>
                      </div>
                      <select value={manualDate} onChange={e=>setManualDate(e.target.value)}
                        className="w-full px-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none font-mono mb-3">
                        {weekDates.map(d=><option key={d.date} value={d.date}>{d.label} — {d.date}</option>)}
                      </select>
                      {manualDone
                        ? <div className="flex items-center gap-2 text-xs text-emerald-600 font-semibold py-1"><BadgeCheck className="w-4 h-4" />Attendance marked</div>
                        : <button onClick={markManual} className="w-full py-2 rounded text-xs font-bold text-white hover:opacity-90" style={{background:"var(--primary)"}}>
                            <Check className="w-3.5 h-3.5 inline mr-1.5" />Mark Present — {weekDates.find(d=>d.date===manualDate)?.label??manualDate}
                          </button>}
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* Log */}
          <div className="lg:col-span-3 bg-card rounded border border-border flex flex-col">
            <div className="px-4 py-3 border-b border-border">
              <div className="flex items-center gap-1.5 flex-wrap">
                <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest mr-1">Day:</span>
                {weekDates.map(d=>(
                  <button key={d.date} onClick={()=>setViewDate(d.date)}
                    className="px-2.5 py-1 rounded text-[11px] font-semibold transition-all border whitespace-nowrap"
                    style={{background:viewDate===d.date?"var(--primary)":"var(--input-background)",color:viewDate===d.date?"#fff":"var(--muted-foreground)",borderColor:"var(--border)"}}>
                    {d.label}
                  </button>
                ))}
              </div>
            </div>
            <div className="px-5 py-2.5 border-b border-border flex items-center justify-between">
              <h3 className="text-xs font-bold text-foreground">Log — {viewDate}{viewDate===todayStr?" (Today)":""}</h3>
              <span className="text-[10px] font-mono text-muted-foreground">
                <span className="text-emerald-600 font-bold">{viewRecs.filter(r=>r.verifiedBy!=="Manual Entry").length}</span> scanned ·{" "}
                <span className="text-sky-600 font-bold">{viewRecs.filter(r=>r.verifiedBy==="Manual Entry").length}</span> manual
              </span>
            </div>
            <div className="overflow-x-auto flex-1">
              <table className="w-full text-xs">
                <thead><tr className="border-b border-border bg-muted/30">
                  {["Docket #","Client Name","Category","Time","Source","Status"].map(h=>(
                    <th key={h} className="text-left px-4 py-2.5 text-[10px] font-bold text-muted-foreground uppercase tracking-wide whitespace-nowrap">{h}</th>
                  ))}
                </tr></thead>
                <tbody className="divide-y divide-border">
                  {viewRecs.sort((a,b)=>a.time.localeCompare(b.time)).map(r=>{
                    const c=clients.find(x=>x.clientId===r.clientId);
                    const isManual=r.verifiedBy==="Manual Entry";
                    return (
                      <tr key={r.attendanceId} onClick={()=>c&&setInfoClient(c)}
                        className={`cursor-pointer hover:bg-muted/20 transition-colors ${isManual?"bg-sky-50/40":""}`}>
                        <td className="px-4 py-2.5 font-mono text-sky-700">{r.docketNumber}</td>
                        <td className="px-4 py-2.5 font-semibold text-foreground">{c?formatName(c):r.fullName}</td>
                        <td className="px-4 py-2.5">{c&&<Badge status={c.clientCategory} />}</td>
                        <td className="px-4 py-2.5 font-mono">{r.time}</td>
                        <td className="px-4 py-2.5">
                          <span className={`text-[10px] font-mono px-1.5 py-0.5 rounded border ${isManual?"text-sky-700 bg-sky-50 border-sky-200":"text-slate-600 bg-slate-50 border-slate-200"}`}>{isManual?"Manual":"FS64"}</span>
                        </td>
                        <td className="px-4 py-2.5"><Badge status="Present" /></td>
                      </tr>
                    );
                  })}
                  {viewRecs.length===0&&<tr><td colSpan={6} className="px-4 py-8 text-center text-muted-foreground font-mono text-[11px]">No records for {viewDate}.</td></tr>}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      {/* Confirmation popup */}
      {state==="confirm"&&matched&&(
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-lg border border-border w-full max-w-sm shadow-2xl">
            <div className="px-6 py-4 border-b border-border flex items-center gap-2 text-sky-600">
              <Fingerprint className="w-5 h-5" /><h2 className="text-sm font-bold">Confirm Attendance</h2>
            </div>
            <div className="p-6">
              <div className="grid grid-cols-2 gap-2 mb-5">
                {[
                  ["Client",formatName(matched)],["Docket",matched.docketNumber],
                  ["Category",matched.clientCategory],["Officer",matched.assignedOfficer],
                  ["Date",todayStr],["Time",pendingTime],["Status","PRESENT"],["Source","Futronic FS64"],
                ].map(([k,v])=>(
                  <div key={k} className="bg-muted rounded p-2">
                    <div className="text-[9px] font-mono uppercase tracking-widest text-muted-foreground mb-0.5">{k}</div>
                    <div className={`text-xs font-semibold ${k==="Status"?"text-emerald-700":""}`}>{v}</div>
                  </div>
                ))}
              </div>
              <div className="flex gap-3">
                <button onClick={()=>{setState("idle");setMatched(null);}} className="flex-1 py-2.5 rounded border border-border text-sm font-semibold text-foreground hover:bg-muted">Cancel</button>
                <button onClick={confirmAttendance} className="flex-1 py-2.5 rounded text-sm font-bold text-white" style={{background:"#16a34a"}}>
                  <Check className="w-4 h-4 inline mr-1.5" />Confirm — Mark Present
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
      {infoClient&&<ClientInfoModal client={infoClient} recs={recs} onClose={()=>setInfoClient(null)} updateRec={updateRec} addRec={addRec} />}
      <style>{`@keyframes scanline{0%,100%{top:22%;opacity:.9}50%{top:68%;opacity:1}}`}</style>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// MONTHLY MATRIX
// ══════════════════════════════════════════════════════════════════════════════
// ── Export utilities ───────────────────────────────────────────────────────────
function exportPPAForm18(exportClients: Client[], exportRecs: AttendanceRecord[], year: number) {
  const MONTH_LETTERS = ["J","F","M","A","M","J","J","A","S","O","N","D"];
  const getCell = (clientId: string, mi: number) => {
    const prefix = `${year}-${String(mi+1).padStart(2,"0")}`;
    if (exportRecs.some(r=>r.clientId===clientId&&r.date.startsWith(prefix))) return "P";
    const now = new Date();
    const curYear = now.getFullYear(); const curM = now.getMonth();
    if (year < curYear) return "A";
    if (year === curYear && mi < curM) return "A";
    return "";
  };

  const rows = exportClients.map(c=>`
    <tr>
      <td class="cell doc">${c.docketNumber}</td>
      <td class="cell name">${c.fullName}${c.middleInitial?" "+c.middleInitial:""}</td>
      ${MONTH_LETTERS.map((_,mi)=>`<td class="cell mon">${getCell(c.clientId,mi)}</td>`).join("")}
      <td class="cell rem">${c.remarks||""}</td>
      <td class="cell eos">${c.supervisionEnd}</td>
    </tr>`).join("");

  // Blank filler rows to match the form's row count
  const blankRows = Math.max(0, 30 - exportClients.length);
  const blanks = Array(blankRows).fill(`
    <tr>
      <td class="cell doc">&nbsp;</td>
      <td class="cell name">&nbsp;</td>
      ${MONTH_LETTERS.map(()=>`<td class="cell mon">&nbsp;</td>`).join("")}
      <td class="cell rem">&nbsp;</td>
      <td class="cell eos">&nbsp;</td>
    </tr>`).join("");

  const html = `<!DOCTYPE html><html><head><meta charset="UTF-8">
  <title>PPA Form 18 - AMFFC ${year}</title>
  <style>
    @page { size: legal landscape; margin: 15mm 12mm; }
    body { font-family: Arial, sans-serif; font-size: 9pt; margin: 0; }
    .header { text-align: center; margin-bottom: 8px; }
    .header .form-no { font-size: 8pt; font-style: italic; font-weight: bold; }
    .header .ppa-form { font-size: 8pt; font-weight: bold; }
    .header .org { font-size: 9pt; }
    .header .title { font-size: 10pt; font-weight: bold; margin-top: 4px; }
    .header .subtitle { font-size: 8pt; font-style: italic; margin-top: 2px; }
    .header .period { font-size: 9pt; font-weight: bold; margin-top: 2px; }
    table { width: 100%; border-collapse: collapse; }
    th, td { border: 1px solid #000; padding: 2px 3px; vertical-align: middle; }
    th { font-size: 7.5pt; font-weight: bold; text-align: center; background: #fff; }
    .cell { font-size: 8pt; }
    .cell.doc { width: 72px; text-align: center; }
    .cell.name { width: 160px; text-align: left; }
    .cell.mon { width: 24px; text-align: center; font-weight: bold; }
    .cell.rem { width: 120px; }
    .cell.eos { width: 72px; text-align: center; font-size: 7.5pt; }
    .th-sup { text-align: center; }
  </style></head><body>
  <div class="header">
    <div style="display:flex;justify-content:space-between;align-items:flex-start;">
      <div class="ppa-form">PPA FORM 18</div>
      <div class="form-no">PPA-FO-FR-018</div>
    </div>
    <div class="org">Republic of the Philippines<br>Department of Justice<br><strong>PAROLE AND PROBATION ADMINISTRATION</strong></div>
    <div class="title">ATTENDANCE MONITORING FORM FOR CLIENTS (AMFFC)</div>
    <div class="subtitle">for CPPO's use</div>
    <div class="period">PERIOD JANUARY - DECEMBER ${year}</div>
  </div>
  <table>
    <thead>
      <tr>
        <th rowspan="2" style="width:72px">DOCKET<br>NO.</th>
        <th rowspan="2" style="width:160px">NAME OF CLIENT</th>
        <th colspan="12" class="th-sup">MONTH</th>
        <th rowspan="2" style="width:120px">REMARKS</th>
        <th rowspan="2" style="width:72px">END OF<br>SUPERVISION</th>
      </tr>
      <tr>
        ${MONTH_LETTERS.map(l=>`<th style="width:24px">${l}</th>`).join("")}
      </tr>
    </thead>
    <tbody>
      ${rows}
      ${blanks}
    </tbody>
  </table>
  </body></html>`;

  const w = window.open("","_blank","width=1200,height=800");
  if (w) { w.document.write(html); w.document.close(); setTimeout(()=>w.print(),400); }
}

function exportPPAForm19(exportClients: Client[], year: number) {
  const rows = exportClients.map(c=>`
    <tr>
      <td class="cell doc">${c.docketNumber}</td>
      <td class="cell name">${c.fullName}${c.middleInitial?" "+c.middleInitial:""}</td>
      <td class="cell cc">${c.ccNumber}</td>
      <td class="cell crt">${c.court}</td>
      <td class="cell dt">${c.supervisionStart}</td>
      <td class="cell dt">${c.supervisionEnd}</td>
      <td class="cell rem">&nbsp;</td>
    </tr>`).join("");

  const blankRows = Math.max(0, 30 - exportClients.length);
  const blanks = Array(blankRows).fill(`
    <tr>
      <td class="cell doc">&nbsp;</td>
      <td class="cell name">&nbsp;</td>
      <td class="cell cc">&nbsp;</td>
      <td class="cell crt">&nbsp;</td>
      <td class="cell dt">&nbsp;</td>
      <td class="cell dt">&nbsp;</td>
      <td class="cell rem">&nbsp;</td>
    </tr>`).join("");

  const html = `<!DOCTYPE html><html><head><meta charset="UTF-8">
  <title>PPA Form 19 - Cases Due for Termination ${year}</title>
  <style>
    @page { size: legal portrait; margin: 15mm 12mm; }
    body { font-family: Arial, sans-serif; font-size: 9pt; margin: 0; }
    .header { text-align: center; margin-bottom: 8px; }
    .header .form-no { font-size: 8pt; font-style: italic; font-weight: bold; }
    .header .ppa-form { font-size: 8pt; font-weight: bold; }
    .top-bar { display: flex; justify-content: space-between; }
    table { width: 100%; border-collapse: collapse; margin-top: 8px; }
    th, td { border: 1px solid #000; padding: 2px 4px; vertical-align: middle; }
    th { font-size: 8pt; font-weight: bold; text-align: center; }
    .cell { font-size: 8.5pt; }
    .cell.doc { width: 70px; text-align: center; }
    .cell.name { width: 180px; }
    .cell.cc { width: 90px; text-align: center; }
    .cell.crt { width: 110px; }
    .cell.dt { width: 70px; text-align: center; font-size: 7.5pt; }
    .cell.rem { width: 110px; }
  </style></head><body>
  <div class="header">
    <div class="top-bar">
      <div class="ppa-form">PPA FORM 19</div>
      <div class="form-no">PPA-FO-FR-019</div>
    </div>
    <div style="font-size:11pt;font-weight:bold;margin-top:10px">SUPERVISING OFFICER'S CASES DUE FOR TERMINATION</div>
    <div style="font-size:10pt;font-weight:bold;margin-top:2px">YEAR ${year}</div>
  </div>
  <table>
    <thead>
      <tr>
        <th rowspan="2" style="width:70px">DOCKET NO.</th>
        <th rowspan="2" style="width:180px">CLIENT'S NAME</th>
        <th rowspan="2" style="width:90px">CC NO.</th>
        <th rowspan="2" style="width:110px">COURT</th>
        <th colspan="2" style="text-align:center">SUPERVISION</th>
        <th rowspan="2" style="width:110px">REMARKS</th>
      </tr>
      <tr>
        <th style="width:70px">STARTED</th>
        <th style="width:70px">TO END</th>
      </tr>
    </thead>
    <tbody>
      ${rows}
      ${blanks}
    </tbody>
  </table>
  </body></html>`;

  const w = window.open("","_blank","width=900,height=900");
  if (w) { w.document.write(html); w.document.close(); setTimeout(()=>w.print(),400); }
}

function HistoryPage({clients,recs,updateRec,addRec}:{clients:Client[];recs:AttendanceRecord[];updateRec?:(id:string,st:string)=>void;addRec?:(r:AttendanceRecord)=>void}) {
  const [selectedYear,setSelectedYear] = useState(YEAR);
  const [quarter,setQuarter] = useState<"Q1"|"Q2"|"Q3"|"Q4"|"All">("Q2");
  const [view,setView] = useState<"matrix"|"list">("matrix");
  const [fOfficer,setFOfficer] = useState("All");
  const [fCategory,setFCategory] = useState("All");
  const [fGender,setFGender] = useState("All");
  const [fSupStatus,setFSupStatus] = useState("All");
  const [showExportMenu,setShowExportMenu] = useState(false);
  const [fDoNdo,setFDoNdo] = useState("All");
  const [showInactive,setShowInactive] = useState(false);
  const [matrixSearch,setMatrixSearch] = useState("");
  const [sortBy,setSortBy] = useState("docket");
  const [sortDir,setSortDir] = useState<"asc"|"desc">("asc");
  const [infoClient,setInfoClient] = useState<Client|null>(null);

  const visibleMonths = quarter==="All"?[0,1,2,3,4,5,6,7,8,9,10,11]:QUARTERS[quarter];
  const toggleSort = (col:string) => { if(sortBy===col)setSortDir(d=>d==="asc"?"desc":"asc");else{setSortBy(col);setSortDir("asc");} };

  const filtered = clients.filter(c=>{
    const st=supStatus(c);
    const isActiveForMatrix=c.status==="Active"||showInactive;
    return isActiveForMatrix
      &&(fOfficer==="All"||c.assignedOfficer===fOfficer)
      &&(fCategory==="All"||c.clientCategory===fCategory)
      &&(fGender==="All"||c.gender===fGender)
      &&(fDoNdo==="All"||c.doNdo===fDoNdo)
      &&(fSupStatus==="All"||st===fSupStatus)
      &&(matrixSearch.length<2||[c.fullName,formatName(c),c.docketNumber,c.ccNumber].some(v=>v.toLowerCase().includes(matrixSearch.toLowerCase())));
  }).sort((a,b)=>{
    if (sortBy==="docket"&&(fCategory==="All")) {
      const po=(PHASE_ORDER[a.supervisionPhase]??99)-(PHASE_ORDER[b.supervisionPhase]??99);
      if (po!==0) return po;
      return a.docketNumber.localeCompare(b.docketNumber);
    }
    const val=(x:Client):string=>({docket:x.docketNumber,name:formatName(x),officer:x.assignedOfficer,category:x.clientCategory,phase:x.supervisionPhase}[sortBy]??"");
    return sortDir==="asc"?val(a).localeCompare(val(b)):val(b).localeCompare(val(a));
  });

  const [listPage,setListPage] = useState(1);
  const perPage=12;
  const allRecs=[...recs].sort((a,b)=>b.date.localeCompare(a.date));
  const totalPages=Math.max(1,Math.ceil(allRecs.length/perPage));
  const pageRecs=allRecs.slice((listPage-1)*perPage,listPage*perPage);

  return (
    <div className="flex-1 flex flex-col overflow-hidden" onClick={()=>showExportMenu&&setShowExportMenu(false)}>
      <Topbar title="Monthly Compliance Matrix" sub={`${selectedYear} — double-click a row for client details`} />
      <div className="flex-1 overflow-y-auto p-5 space-y-4">
        <div className="bg-card rounded border border-border p-4 space-y-3">
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center gap-1.5">
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Year:</span>
              <select value={selectedYear} onChange={e=>{setSelectedYear(Number(e.target.value));setQuarter("Q1");}}
                className="px-2.5 py-1.5 text-[11px] rounded border border-border bg-input-background text-foreground focus:outline-none font-mono font-semibold">
                {AVAILABLE_YEARS.map(y=><option key={y} value={y}>{y}</option>)}
              </select>
            </div>
            <div className="flex items-center gap-1">
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest mr-1">Quarter:</span>
              {(["Q1","Q2","Q3","Q4","All"] as const).map(q=>(
                <button key={q} onClick={()=>setQuarter(q)}
                  className="px-3 py-1.5 rounded text-[11px] font-semibold transition-all border"
                  style={{background:quarter===q?"var(--primary)":"var(--input-background)",color:quarter===q?"#fff":"var(--muted-foreground)",borderColor:"var(--border)"}}>
                  {q}
                </button>
              ))}
            </div>
            <div className="flex items-center gap-1 ml-auto">
              {([["matrix","Compliance Matrix"],["list","List View"]] as [string,string][]).map(([v,l])=>(
                <button key={v} onClick={()=>setView(v as any)}
                  className="px-3 py-1.5 rounded text-[11px] font-semibold transition-all border"
                  style={{background:view===v?"var(--primary)":"var(--input-background)",color:view===v?"#fff":"var(--muted-foreground)",borderColor:"var(--border)"}}>
                  {l}
                </button>
              ))}
            </div>
          </div>
          {view==="matrix"&&(
            <div className="flex flex-wrap items-center gap-3">
              {/* Unified sort bar */}
              <SortBar label="Sort:"
                options={[["docket","Docket"],["name","Name"],["category","Category"],["phase","Phase"]] as [string,string][] as any}
                sortBy={sortBy} sortDir={sortDir} onToggle={(k:any)=>toggleSort(k)}
              />
              <div className="w-px h-5 bg-border flex-shrink-0" />
              <div className="relative min-w-44">
                <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input value={matrixSearch} onChange={e=>setMatrixSearch(e.target.value)} placeholder="Search clients…"
                  className="w-full pl-8 pr-3 py-1.5 text-[11px] rounded border border-border bg-input-background text-foreground focus:outline-none font-mono" />
              </div>
              <SlidersHorizontal className="w-3.5 h-3.5 text-muted-foreground flex-shrink-0" />
              {[
                {l:"Officer",v:fOfficer,set:setFOfficer,opts:["All",...OFFICERS]},
                {l:"Category",v:fCategory,set:setFCategory,opts:["All","Probationer","Parolee","Pardonee"]},
                {l:"Gender",v:fGender,set:setFGender,opts:["All","Male","Female"]},
                {l:"DO/NDO",v:fDoNdo,set:setFDoNdo,opts:["All","DO","NDO"]},
                {l:"Sup. Status",v:fSupStatus,set:setFSupStatus,opts:["All","Active","Near Expiry","Ended","Completed","Terminated"] as string[]},
              ].map(({l,v,set,opts})=>(
                <div key={l} className="flex items-center gap-1.5">
                  <label className="text-[10px] font-semibold text-muted-foreground">{l}:</label>
                  <select value={v} onChange={e=>set(e.target.value)}
                    className="px-2 py-1 text-[11px] rounded border border-border bg-input-background text-foreground focus:outline-none font-mono">
                    {opts.map(o=><option key={o}>{o}</option>)}
                  </select>
                </div>
              ))}
              <label className="flex items-center gap-1.5 cursor-pointer ml-auto">
                <input type="checkbox" checked={showInactive} onChange={e=>setShowInactive(e.target.checked)} className="rounded" />
                <span className="text-[11px] text-muted-foreground font-semibold">Show Inactive</span>
              </label>
              {/* Export dropdown */}
              <div className="relative">
                <button
                  onClick={()=>setShowExportMenu(v=>!v)}
                  className="flex items-center gap-1.5 px-3 py-1.5 text-[11px] font-semibold rounded border-2 border-sky-500 bg-sky-500 text-white hover:bg-sky-600 transition-colors">
                  <Download className="w-3.5 h-3.5" />Export <ChevronDown className="w-3 h-3 ml-0.5" />
                </button>
                {showExportMenu&&(
                  <div className="absolute right-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-xl w-72" onClick={e=>e.stopPropagation()}>
                    <div className="px-4 py-2.5 border-b border-border">
                      <p className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Select Export Template</p>
                    </div>
                    <div className="p-2 space-y-1">
                      <button
                        onClick={()=>{setShowExportMenu(false);exportPPAForm18(filtered,recs,selectedYear);}}
                        className="w-full text-left px-3 py-3 rounded-md hover:bg-sky-50 hover:border-sky-200 border border-transparent transition-all group">
                        <div className="flex items-start gap-3">
                          <div className="w-8 h-8 rounded bg-sky-100 flex items-center justify-center flex-shrink-0 mt-0.5 group-hover:bg-sky-200">
                            <FileText className="w-4 h-4 text-sky-600" />
                          </div>
                          <div>
                            <p className="text-xs font-bold text-foreground">Attendance Monitoring Form</p>
                            <p className="text-[10px] text-muted-foreground">PPA Form 18 — AMFFC (PPA-FO-FR-018)</p>
                            <p className="text-[10px] text-sky-600 mt-0.5">Monthly attendance matrix · Jan–Dec {selectedYear}</p>
                          </div>
                        </div>
                      </button>
                      <button
                        onClick={()=>{setShowExportMenu(false);exportPPAForm19(filtered,selectedYear);}}
                        className="w-full text-left px-3 py-3 rounded-md hover:bg-violet-50 hover:border-violet-200 border border-transparent transition-all group">
                        <div className="flex items-start gap-3">
                          <div className="w-8 h-8 rounded bg-violet-100 flex items-center justify-center flex-shrink-0 mt-0.5 group-hover:bg-violet-200">
                            <FileText className="w-4 h-4 text-violet-600" />
                          </div>
                          <div>
                            <p className="text-xs font-bold text-foreground">Cases Due for Termination</p>
                            <p className="text-[10px] text-muted-foreground">PPA Form 19 (PPA-FO-FR-019)</p>
                            <p className="text-[10px] text-violet-600 mt-0.5">Supervision periods · {selectedYear}</p>
                          </div>
                        </div>
                      </button>
                    </div>
                    <div className="px-4 py-2 border-t border-border">
                      <p className="text-[10px] text-muted-foreground">Exports current filtered view ({filtered.length} clients). Print dialog will open automatically.</p>
                    </div>
                  </div>
                )}
              </div>
              <button className="flex items-center gap-1.5 px-3 py-1.5 text-[11px] font-semibold rounded border border-border text-muted-foreground hover:bg-muted"><Printer className="w-3.5 h-3.5" />Print</button>
            </div>
          )}
        </div>

        {view==="matrix"&&(
          <>
            <div className="flex items-center gap-4 text-[11px]">
              <span className="text-muted-foreground font-mono font-semibold">Legend:</span>
              <span className="flex items-center gap-1.5"><span className="w-5 h-5 rounded bg-emerald-100 border border-emerald-300 flex items-center justify-center text-emerald-700 font-bold text-[10px]">P</span>Present</span>
              <span className="flex items-center gap-1.5"><span className="w-5 h-5 rounded bg-red-100 border border-red-200 flex items-center justify-center text-red-600 font-bold text-[10px]">A</span>Absent</span>
              <span className="flex items-center gap-1.5"><span className="w-5 h-5 rounded bg-muted border border-border flex items-center justify-center text-muted-foreground text-[10px]">—</span>Pending</span>
              <span className="flex items-center gap-2 ml-4">
                <span className="w-2 h-2 rounded-full bg-emerald-500 inline-block" />Active
                <span className="w-2 h-2 rounded-full bg-amber-400 inline-block ml-2" />Near Expiry
                <span className="w-2 h-2 rounded-full bg-red-500 inline-block ml-2" />Ended
              </span>
              <div className="ml-auto flex items-center gap-2">
                <span className="text-base font-bold text-foreground">{filtered.length}</span>
                <span className="text-xs text-muted-foreground">clients</span>
                <span className="w-px h-4 bg-border" />
                <span className="text-sm font-bold text-blue-600">{filtered.filter(c=>c.gender==="Male").length}M</span>
                <span className="text-xs text-muted-foreground">/</span>
                <span className="text-sm font-bold text-pink-600">{filtered.filter(c=>c.gender==="Female").length}F</span>
              </div>
            </div>
            <div className="bg-card rounded border border-border overflow-x-auto">
              <table className="text-xs min-w-full">
                <thead>
                  <tr className="border-b border-border bg-muted/40">
                    <th className="text-left px-3 py-3 text-[10px] font-bold text-muted-foreground uppercase w-8">#</th>
                    {[{k:"docket",l:"Docket #"},{k:"name",l:"Name"},{k:"category",l:"Category"},{k:"phase",l:"Phase"}].map(({k,l})=>(
                      <th key={k} className="text-left px-3 py-3 text-[10px] font-bold text-muted-foreground uppercase tracking-wide cursor-pointer select-none whitespace-nowrap hover:text-foreground transition-colors" onClick={()=>toggleSort(k)}>
                        <span className="flex items-center gap-1">{l}<SortIcon col={k} cur={sortBy} dir={sortDir} /></span>
                      </th>
                    ))}
                    <th className="text-left px-3 py-3 text-[10px] font-bold text-muted-foreground uppercase whitespace-nowrap">Officer</th>
                    <th className="text-left px-3 py-3 text-[10px] font-bold text-muted-foreground uppercase whitespace-nowrap">Sup. Status</th>
                    {visibleMonths.map(mi=>(
                      <th key={mi} className="text-center px-1 py-3 text-[10px] font-bold text-muted-foreground uppercase w-10">{MONTHS[mi]}</th>
                    ))}
                    <th className="text-center px-2 py-3 text-[10px] font-bold text-muted-foreground uppercase w-14">Total</th>
                    <th className="text-left px-3 py-3 text-[10px] font-bold text-muted-foreground uppercase">Remarks</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {filtered.map((c,ri)=>{
                    const statuses=visibleMonths.map(mi=>cellStatus(c,mi,recs,selectedYear));
                    const presentCount=statuses.filter(s=>s==="P").length;
                    const st=supStatus(c);
                    return (
                      <tr key={c.clientId} onDoubleClick={()=>setInfoClient(c)}
                        className={`cursor-pointer transition-colors hover:bg-sky-50 ${c.status!=="Active"?"opacity-60":""}`}>
                        <td className="px-3 py-2.5 text-muted-foreground font-mono">{ri+1}</td>
                        <td className="px-3 py-2.5 font-mono text-sky-700 whitespace-nowrap">{c.docketNumber}</td>
                        <td className="px-3 py-2.5 whitespace-nowrap font-semibold text-foreground">{formatName(c)}</td>
                        <td className="px-3 py-2.5"><Badge status={c.clientCategory} /></td>
                        <td className="px-3 py-2.5 text-muted-foreground whitespace-nowrap">{c.supervisionPhase}</td>
                        <td className="px-3 py-2.5 text-muted-foreground whitespace-nowrap text-[11px]">{c.assignedOfficer}</td>
                        <td className="px-3 py-2.5"><SupBadge client={c} /></td>
                        {statuses.map((st2,si)=>(
                          <td key={si} className="px-1 py-2.5 text-center">
                            {st2==="P"&&<span className="w-6 h-6 rounded bg-emerald-100 border border-emerald-300 flex items-center justify-center mx-auto text-emerald-700 text-[10px] font-bold">P</span>}
                            {st2==="A"&&<span className="w-6 h-6 rounded bg-red-100 border border-red-200 flex items-center justify-center mx-auto text-red-600 text-[10px] font-bold">A</span>}
                            {st2==="-"&&<span className="text-muted-foreground text-[10px] font-mono">—</span>}
                          </td>
                        ))}
                        <td className="px-2 py-2.5 text-center">
                          <span className={`font-mono font-bold text-[11px] ${presentCount===visibleMonths.length?"text-emerald-600":presentCount===0?"text-red-500":"text-amber-600"}`}>
                            {presentCount}/{visibleMonths.length}
                          </span>
                        </td>
                        <td className="px-3 py-2.5 max-w-[120px]">
                          <span className={`text-[11px] truncate block ${!c.remarks&&!c.finalReport?"text-red-400 italic":"text-muted-foreground"}`}>
                            {c.remarks||c.finalReport||"No remarks"}
                          </span>
                        </td>
                      </tr>
                    );
                  })}
                  {filtered.length===0&&<tr><td colSpan={10+visibleMonths.length} className="px-4 py-8 text-center text-muted-foreground font-mono text-[11px]">No records match.</td></tr>}
                </tbody>
              </table>
            </div>
          </>
        )}

        {view==="list"&&(
          <div className="bg-card rounded border border-border">
            <div className="px-5 py-3 border-b border-border flex items-center justify-between">
              <span className="text-xs font-mono text-muted-foreground">{allRecs.length} records</span>
              <div className="flex gap-2">
                <button className="flex items-center gap-1.5 px-3 py-1.5 text-[11px] font-semibold rounded border border-border text-muted-foreground hover:bg-muted"><Download className="w-3.5 h-3.5" />CSV</button>
                <button className="flex items-center gap-1.5 px-3 py-1.5 text-[11px] font-semibold rounded border border-border text-muted-foreground hover:bg-muted"><Printer className="w-3.5 h-3.5" />Print</button>
              </div>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead><tr className="border-b border-border bg-muted/30">
                  {["Date","Time","Docket #","Full Name","Category","Officer","Source"].map(h=>(
                    <th key={h} className="text-left px-4 py-2.5 text-[10px] font-bold text-muted-foreground uppercase tracking-wide whitespace-nowrap">{h}</th>
                  ))}
                </tr></thead>
                <tbody className="divide-y divide-border">
                  {pageRecs.map(r=>{
                    const c=clients.find(x=>x.clientId===r.clientId);
                    return (
                      <tr key={r.attendanceId} onClick={()=>c&&setInfoClient(c)} className="cursor-pointer hover:bg-muted/20">
                        <td className="px-4 py-2.5 font-mono">{r.date}</td>
                        <td className="px-4 py-2.5 font-mono">{r.time}</td>
                        <td className="px-4 py-2.5 font-mono text-sky-700">{r.docketNumber}</td>
                        <td className="px-4 py-2.5 font-semibold">{c?formatName(c):r.fullName}</td>
                        <td className="px-4 py-2.5">{c&&<Badge status={c.clientCategory} />}</td>
                        <td className="px-4 py-2.5 text-muted-foreground">{r.verifiedBy}</td>
                        <td className="px-4 py-2.5"><span className={`text-[10px] font-mono px-1.5 py-0.5 rounded border ${r.verifiedBy==="Manual Entry"?"text-sky-700 bg-sky-50 border-sky-200":"text-slate-600 bg-slate-50 border-slate-200"}`}>{r.verifiedBy==="Manual Entry"?"Manual":"FS64"}</span></td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className="px-5 py-3 border-t border-border flex items-center justify-between">
              <span className="text-[11px] font-mono text-muted-foreground">Page {listPage} of {totalPages}</span>
              <div className="flex items-center gap-1">
                <button onClick={()=>setListPage(p=>Math.max(1,p-1))} disabled={listPage===1} className="p-1.5 rounded border border-border text-muted-foreground hover:bg-muted disabled:opacity-40"><ChevronLeft className="w-3.5 h-3.5" /></button>
                {Array.from({length:Math.min(5,totalPages)},(_,i)=>i+1).map(n=>(
                  <button key={n} onClick={()=>setListPage(n)} className={`w-7 h-7 rounded border text-[11px] font-mono ${n===listPage?"border-sky-500 bg-sky-50 text-sky-700":"border-border text-muted-foreground hover:bg-muted"}`}>{n}</button>
                ))}
                <button onClick={()=>setListPage(p=>Math.min(totalPages,p+1))} disabled={listPage>=totalPages} className="p-1.5 rounded border border-border text-muted-foreground hover:bg-muted disabled:opacity-40"><ChevronRight className="w-3.5 h-3.5" /></button>
              </div>
            </div>
          </div>
        )}
      </div>
      {infoClient&&<ClientInfoModal client={infoClient} recs={recs} onClose={()=>setInfoClient(null)} updateRec={updateRec} addRec={addRec} />}
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// DATABASE / SEARCH
// ══════════════════════════════════════════════════════════════════════════════
function SearchPage({clients,recs,updateRec,addRec}:{clients:Client[];recs:AttendanceRecord[];updateRec?:(id:string,st:string)=>void;addRec?:(r:AttendanceRecord)=>void}) {
  const [q,setQ] = useState("");
  const [fOfficer,setFOfficer] = useState("All");
  const [fCat,setFCat] = useState("All");
  const [fStatus,setFStatus] = useState("All");
  const [fGender,setFGender] = useState("All");
  const [fRemarks,setFRemarks] = useState("All");
  const [modalClient,setModalClient] = useState<Client|null>(null);
  const [sortBy,setSortBy] = useState<"docket"|"name"|"case"|"phase">("docket");
  const [sortDir,setSortDir] = useState<"asc"|"desc">("asc");

  const toggleSort=(col:typeof sortBy)=>{if(sortBy===col)setSortDir(d=>d==="asc"?"desc":"asc");else{setSortBy(col);setSortDir("asc");}};

  const filtered = clients.filter(c=>{
    const mq=[c.fullName,formatName(c),c.clientId,c.ccNumber,c.docketNumber,c.court].some(v=>v.toLowerCase().includes(q.toLowerCase()));
    const mRemarks=fRemarks==="All"||(fRemarks==="With Remarks"&&!!(c.remarks||c.finalReport||c.violationReport))||(fRemarks==="Without Remarks"&&!(c.remarks||c.finalReport||c.violationReport));
    return mq&&(fOfficer==="All"||c.assignedOfficer===fOfficer)&&(fCat==="All"||c.clientCategory===fCat)&&(fStatus==="All"||c.status===fStatus)&&(fGender==="All"||c.gender===fGender)&&mRemarks;
  }).sort((a,b)=>{
    const val=(x:Client)=>(sortBy==="docket"?x.docketNumber:sortBy==="name"?formatName(x):sortBy==="case"?x.ccNumber:x.supervisionPhase);
    return sortDir==="asc"?val(a).localeCompare(val(b)):val(b).localeCompare(val(a));
  });

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      <Topbar title="Database / Search" sub="Client records lookup — click a row to view full details" />
      <div className="flex-1 overflow-y-auto p-5 space-y-4">
        <div className="bg-card rounded border border-border p-4 flex flex-wrap gap-3 items-end">
          <div className="flex-1 min-w-48 relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input value={q} onChange={e=>setQ(e.target.value)} placeholder="Name, Docket #, CC Number, Court…"
              className="w-full pl-8 pr-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none font-mono" />
          </div>
          {[
            {l:"Officer",v:fOfficer,set:setFOfficer,opts:["All",...OFFICERS]},
            {l:"Category",v:fCat,set:setFCat,opts:["All","Probationer","Parolee","Pardonee"]},
            {l:"Gender",v:fGender,set:setFGender,opts:["All","Male","Female"]},
            {l:"Status",v:fStatus,set:setFStatus,opts:["All","Active","Completed","Terminated","Inactive"]},
            {l:"Remarks",v:fRemarks,set:setFRemarks,opts:["All","With Remarks","Without Remarks"]},
          ].map(({l,v,set,opts})=>(
            <div key={l}>
              <label className="block text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-1">{l}</label>
              <select value={v} onChange={e=>set(e.target.value)}
                className="px-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none font-mono">
                {opts.map(o=><option key={o}>{o}</option>)}
              </select>
            </div>
          ))}
          <button className="flex items-center gap-1.5 px-3 py-2 text-xs font-semibold rounded border border-border text-muted-foreground hover:bg-muted"><Download className="w-3.5 h-3.5" />Export</button>
        </div>

        {/* Unified sort + summary bar */}
        <div className="bg-card rounded border border-border p-3 flex items-center gap-4 flex-wrap">
          <SortBar
            options={[["docket","Docket #"],["name","Name"],["case","CC No."],["phase","Phase"]] as ["docket"|"name"|"case"|"phase",string][]}
            sortBy={sortBy} sortDir={sortDir} onToggle={toggleSort}
          />
          <div className="ml-auto flex items-center gap-3">
            <span className="text-sm font-bold text-foreground">{filtered.length}</span>
            <span className="text-xs text-muted-foreground">records</span>
            <span className="w-px h-4 bg-border" />
            <span className="text-sm font-bold text-blue-600">{filtered.filter(c=>c.gender==="Male").length}M</span>
            <span className="text-xs text-muted-foreground">/</span>
            <span className="text-sm font-bold text-pink-600">{filtered.filter(c=>c.gender==="Female").length}F</span>
          </div>
        </div>

        <div className="bg-card rounded border border-border">
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead><tr className="border-b border-border bg-muted/30">
                {["Docket #","Full Name","Category","Gender","CC Number","Court","Officer","Sup. Status","Remarks",""].map(h=>(
                  <th key={h} className="text-left px-4 py-2.5 text-[10px] font-bold text-muted-foreground uppercase tracking-wide whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody className="divide-y divide-border">
                {filtered.map(c=>{
                  const hasRemarks=!!(c.remarks||c.finalReport||c.violationReport);
                  return (
                    <tr key={c.clientId} onClick={()=>setModalClient(c)} className="cursor-pointer hover:bg-sky-50 transition-colors">
                      <td className="px-4 py-2.5 font-mono text-sky-700">{c.docketNumber}</td>
                      <td className="px-4 py-2.5 font-semibold whitespace-nowrap">{formatName(c)}</td>
                      <td className="px-4 py-2.5"><Badge status={c.clientCategory} /></td>
                      <td className="px-4 py-2.5"><Badge status={c.gender} /></td>
                      <td className="px-4 py-2.5 font-mono text-muted-foreground">{c.ccNumber||"—"}</td>
                      <td className="px-4 py-2.5 text-muted-foreground max-w-[140px] truncate">{c.court||"—"}</td>
                      <td className="px-4 py-2.5 text-muted-foreground whitespace-nowrap">{c.assignedOfficer}</td>
                      <td className="px-4 py-2.5"><SupBadge client={c} /></td>
                      <td className="px-4 py-2.5">
                        {hasRemarks
                          ? <span className="text-[10px] text-emerald-700 font-mono">With Remarks</span>
                          : <span className="text-[10px] text-red-500 font-mono italic">Missing</span>}
                      </td>
                      <td className="px-4 py-2.5"><Eye className="w-3.5 h-3.5 text-muted-foreground" /></td>
                    </tr>
                  );
                })}
                {filtered.length===0&&<tr><td colSpan={10} className="px-4 py-8 text-center text-muted-foreground font-mono text-[11px]">No records found.</td></tr>}
              </tbody>
            </table>
          </div>
        </div>
      </div>
      {modalClient&&<ClientInfoModal client={modalClient} recs={recs} onClose={()=>setModalClient(null)} updateRec={updateRec} addRec={addRec} />}
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// REPORTS
// ══════════════════════════════════════════════════════════════════════════════
function ReportsPage({clients,recs,updateRec,addRec}:{clients:Client[];recs:AttendanceRecord[];updateRec?:(id:string,st:string)=>void;addRec?:(r:AttendanceRecord)=>void}) {
  const [type,setType] = useState<"monthly"|"daily"|"individual">("monthly");
  const [selClient,setSelClient] = useState(clients.find(c=>c.status==="Active")?.clientId??"");
  const [clientSearch,setClientSearch] = useState("");
  const [showClientDrop,setShowClientDrop] = useState(false);
  const active=clients.filter(c=>c.status==="Active");
  const monthRows=getMonthlyChartData(clients, recs, YEAR).map(m=>({...m,absent:m.total-m.attended,rate:`${Math.round((m.attended/m.total)*100)}%`}));
  const dailyRows=[0,1,2,7].map(d=>{
    const dt=new Date(_today);dt.setDate(dt.getDate()-d);
    const ds=dt.toISOString().split("T")[0];
    const cnt=new Set(recs.filter(r=>r.date===ds).map(r=>r.clientId)).size;
    return {date:ds,present:cnt,absent:active.length-cnt,pct:Math.round((cnt/active.length)*100)};
  });
  const indiv=clients.find(c=>c.clientId===selClient);
  const indivRecs=recs.filter(r=>r.clientId===selClient).sort((a,b)=>b.date.localeCompare(a.date));

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      <Topbar title="Reports" sub="Generate and export attendance reports" />
      <div className="flex-1 overflow-y-auto p-5 space-y-4">
        <div className="bg-card rounded border border-border p-4 flex flex-wrap gap-3 items-center">
          <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Report Type:</span>
          {(["monthly","daily","individual"] as const).map(t=>(
            <button key={t} onClick={()=>setType(t)}
              className="px-4 py-1.5 rounded text-xs font-semibold capitalize transition-all border"
              style={{background:type===t?"var(--primary)":"var(--input-background)",color:type===t?"#fff":"var(--muted-foreground)",borderColor:"var(--border)"}}>
              {t==="individual"?"Individual Client":t.charAt(0).toUpperCase()+t.slice(1)}
            </button>
          ))}
          <div className="ml-auto flex gap-2">
            {[[Printer,"Print"],[FileText,"Excel"],[FileText,"PDF"]].map(([Icon,l],i)=>(
              <button key={i} className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded border border-border text-muted-foreground hover:bg-muted">
                <Icon className="w-3.5 h-3.5" />{l as string}
              </button>
            ))}
          </div>
        </div>

        <div className="bg-card rounded border border-border p-4 flex items-start justify-between">
          <div>
            <div className="flex items-center gap-2 mb-1"><Shield className="w-3.5 h-3.5 text-muted-foreground" /><span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Republic of the Philippines — DOJ — Parole and Probation Administration</span></div>
            <h2 className="text-base font-bold text-foreground">Tagaytay Parole and Probation Office</h2>
            <p className="text-xs text-muted-foreground">{type==="monthly"?`Monthly Attendance Summary — ${YEAR}`:type==="daily"?"Daily Attendance Report":`Individual History — ${indiv?formatName(indiv):""}`}</p>
          </div>
          <div className="text-right text-[10px] font-mono text-muted-foreground"><p>Generated: {todayStr}</p><p>AMS v3.0</p></div>
        </div>

        {type==="monthly"&&(
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <div className="lg:col-span-2 bg-card rounded border border-border p-5">
              <h3 className="text-xs font-bold text-foreground mb-4 uppercase tracking-wide">Attendance Trend {YEAR}</h3>
              <ResponsiveContainer width="100%" height={200}>
                <LineChart data={getMonthlyChartData(clients, recs, YEAR)}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e4eaf2" />
                  <XAxis dataKey="month" tick={{fontSize:11,fill:"#5d7290",fontFamily:"JetBrains Mono"}} axisLine={false} tickLine={false} />
                  <YAxis tick={{fontSize:11,fill:"#5d7290",fontFamily:"JetBrains Mono"}} axisLine={false} tickLine={false} />
                  <Tooltip contentStyle={{fontSize:12,borderRadius:6,border:"1px solid #e4eaf2"}} />
                  <Line type="monotone" dataKey="attended" stroke="#16a34a" strokeWidth={2} dot={{r:4}} name="Attended" />
                  <Line type="monotone" dataKey="total" stroke="#dce8f5" strokeWidth={1.5} strokeDasharray="4 4" name="Total" />
                </LineChart>
              </ResponsiveContainer>
            </div>
            <div className="bg-card rounded border border-border">
              <div className="px-5 py-3 border-b border-border"><h3 className="text-xs font-bold text-foreground uppercase tracking-wide">Summary</h3></div>
              <table className="w-full text-xs">
                <thead><tr className="border-b border-border bg-muted/30">{["Month","Present","Absent","Rate"].map(h=><th key={h} className="text-left px-4 py-2.5 text-[10px] font-bold text-muted-foreground uppercase tracking-wide">{h}</th>)}</tr></thead>
                <tbody className="divide-y divide-border">
                  {monthRows.map(r=>(
                    <tr key={r.month} className="hover:bg-muted/20">
                      <td className="px-4 py-2 font-mono font-semibold">{r.month}</td>
                      <td className="px-4 py-2 font-mono text-emerald-700">{r.attended}</td>
                      <td className="px-4 py-2 font-mono text-red-600">{r.absent}</td>
                      <td className="px-4 py-2 font-mono font-bold">{r.rate}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {type==="daily"&&(
          <div className="bg-card rounded border border-border">
            <div className="px-5 py-3 border-b border-border"><h3 className="text-xs font-bold text-foreground uppercase tracking-wide">Daily Summary</h3></div>
            <table className="w-full text-xs">
              <thead><tr className="border-b border-border bg-muted/30">{["Date","Active Clients","Present","Absent","Compliance"].map(h=><th key={h} className="text-left px-5 py-2.5 text-[10px] font-bold text-muted-foreground uppercase tracking-wide">{h}</th>)}</tr></thead>
              <tbody className="divide-y divide-border">
                {dailyRows.map(r=>(
                  <tr key={r.date} className="hover:bg-muted/20">
                    <td className="px-5 py-3 font-mono font-semibold">{r.date}</td>
                    <td className="px-5 py-3 font-mono">{active.length}</td>
                    <td className="px-5 py-3 font-mono text-emerald-700 font-bold">{r.present}</td>
                    <td className="px-5 py-3 font-mono text-red-600">{r.absent}</td>
                    <td className="px-5 py-3"><div className="flex items-center gap-2"><div className="flex-1 h-1.5 bg-muted rounded-full w-24"><div className="h-1.5 bg-emerald-500 rounded-full" style={{width:`${r.pct}%`}} /></div><span className="font-mono font-bold text-[11px]">{r.pct}%</span></div></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {type==="individual"&&(
          <div className="space-y-4">
            <div className="bg-card rounded border border-border p-4 flex items-start gap-3">
              <label className="text-xs font-bold text-muted-foreground uppercase tracking-widest mt-2 flex-shrink-0">Client:</label>
              <div className="flex-1 max-w-sm relative">
                <div className="relative">
                  <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                  <input value={clientSearch} onChange={e=>{setClientSearch(e.target.value);setShowClientDrop(true);}} onFocus={()=>setShowClientDrop(true)}
                    placeholder="Type name, docket #, or CC number…"
                    className="w-full pl-8 pr-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none font-mono" />
                </div>
                {selClient&&(()=>{const sc=clients.find(c=>c.clientId===selClient);return sc?(
                  <div className="mt-2 flex items-center gap-2 px-3 py-2 bg-sky-50 border border-sky-200 rounded">
                    <div className="flex-1 min-w-0"><p className="text-xs font-bold text-foreground truncate">{formatName(sc)}</p><p className="text-[10px] font-mono text-muted-foreground">{sc.docketNumber} · {sc.ccNumber}</p></div>
                    <button onClick={()=>{setSelClient("");setClientSearch("");}} className="text-muted-foreground hover:text-foreground flex-shrink-0"><X className="w-3.5 h-3.5" /></button>
                  </div>
                ):null;})()}
                {showClientDrop&&clientSearch.length>0&&(
                  <div className="absolute z-10 top-full left-0 right-0 mt-1 bg-card border border-border rounded shadow-lg max-h-48 overflow-y-auto">
                    {clients.filter(c=>[c.fullName,formatName(c),c.docketNumber,c.ccNumber].some(v=>v.toLowerCase().includes(clientSearch.toLowerCase()))).map(c=>(
                      <button key={c.clientId} onMouseDown={()=>{setSelClient(c.clientId);setClientSearch("");setShowClientDrop(false);}}
                        className="w-full px-3 py-2.5 text-left hover:bg-muted/40 border-b border-border last:border-b-0">
                        <p className="text-xs font-semibold text-foreground">{formatName(c)}</p>
                        <p className="text-[10px] font-mono text-muted-foreground">{c.docketNumber} · {c.clientCategory} · {c.assignedOfficer}</p>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>
            {indiv&&(
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                <div className="bg-card rounded border border-border p-5">
                  <div className="mb-4">
                    <p className="text-sm font-bold text-foreground">{formatName(indiv)}</p>
                    <p className="text-[10px] font-mono text-sky-700">{indiv.docketNumber}</p>
                    <div className="flex gap-1.5 mt-1 flex-wrap"><Badge status={indiv.clientCategory} /><Badge status={indiv.gender} /><SupBadge client={indiv} /></div>
                  </div>
                  <div className="space-y-1.5 text-[11px]">
                    {[["Officer",indiv.assignedOfficer],["Phase",indiv.supervisionPhase],["DO/NDO",indiv.doNdo],["Total Records",String(indivRecs.length)]].map(([k,v])=>(
                      <div key={k} className="flex justify-between"><span className="text-muted-foreground">{k}:</span><span className="font-semibold">{v}</span></div>
                    ))}
                    <div className="pt-2 border-t border-border flex justify-between"><span className="text-muted-foreground">Supervision End:</span><span className={`font-mono font-bold ${["Ended","Near Expiry"].includes(supStatus(indiv))?supStatus(indiv)==="Ended"?"text-red-600":"text-amber-600":""}`}>{indiv.supervisionEnd}</span></div>
                  </div>
                </div>
                <div className="lg:col-span-2 bg-card rounded border border-border">
                  <div className="px-5 py-3 border-b border-border"><h3 className="text-xs font-bold text-foreground uppercase tracking-wide">Attendance Log</h3></div>
                  <div className="divide-y divide-border max-h-72 overflow-y-auto">
                    {indivRecs.length>0 ? indivRecs.map(r=>(
                      <div key={r.attendanceId} className="px-5 py-2.5 flex items-center gap-4 hover:bg-muted/20">
                        <span className="font-mono text-[11px] text-muted-foreground w-24">{r.date}</span>
                        <span className="font-mono text-[11px]">{r.time}</span>
                        <span className="flex-1 text-[11px] text-muted-foreground">{r.verifiedBy}</span>
                        <Badge status="Present" />
                      </div>
                    )):<div className="px-5 py-8 text-center text-muted-foreground font-mono text-[11px]">No records.</div>}
                  </div>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// CLIENT MANAGEMENT
// ══════════════════════════════════════════════════════════════════════════════
function ManagementPage({clients,setClients,recs,updateRec,addRec}:{clients:Client[];setClients:React.Dispatch<React.SetStateAction<Client[]>>;recs:AttendanceRecord[];updateRec?:(id:string,st:string)=>void;addRec?:(r:AttendanceRecord)=>void}) {
  const [showForm,setShowForm] = useState(false);
  const [editClient,setEditClient] = useState<Client|undefined>(undefined);
  const [viewClient,setViewClient] = useState<Client|null>(null);
  const [deleteId,setDeleteId] = useState<string|null>(null);
  const [deleteInactiveConfirm,setDeleteInactiveConfirm] = useState(false);
  const [showInactive,setShowInactive] = useState(true);
  const [search,setSearch] = useState("");
  const [fCat,setFCat] = useState("All");
  const [fOfficer,setFOfficer] = useState("All");
  const [fGender,setFGender] = useState("All");
  const [fRemarks,setFRemarks] = useState("All");
  const [sortBy,setSortBy] = useState<"docket"|"name"|"case"|"phase">("docket");
  const [sortDir,setSortDir] = useState<"asc"|"desc">("asc");
  const toggleSort=(col:typeof sortBy)=>{if(sortBy===col)setSortDir(d=>d==="asc"?"desc":"asc");else{setSortBy(col);setSortDir("asc");}};

  const nextId=`TPP-${String(clients.length+1).padStart(4,"0")}`;
  const inactiveCount=clients.filter(c=>c.status!=="Active").length;
  const endedActive=clients.filter(c=>c.status==="Active"&&supStatus(c)==="Ended");
  const nearExpiryActive=clients.filter(c=>c.status==="Active"&&supStatus(c)==="Near Expiry");

  const filtered = clients.filter(c=>{
    const matchStatus=showInactive||c.status==="Active";
    const matchQ=[c.fullName,formatName(c),c.docketNumber,c.ccNumber].some(v=>v.toLowerCase().includes(search.toLowerCase()));
    const matchCat=fCat==="All"||c.clientCategory===fCat;
    const matchOfficer=fOfficer==="All"||c.assignedOfficer===fOfficer;
    const matchGender=fGender==="All"||c.gender===fGender;
    const hasRemarks=!!(c.remarks||c.finalReport||c.violationReport);
    const matchRemarks=fRemarks==="All"||(fRemarks==="With Remarks"&&hasRemarks)||(fRemarks==="Without Remarks"&&!hasRemarks);
    return matchStatus&&matchQ&&matchCat&&matchOfficer&&matchGender&&matchRemarks;
  }).sort((a,b)=>{
    const val=(x:Client)=>(sortBy==="docket"?x.docketNumber:sortBy==="name"?formatName(x):sortBy==="case"?x.ccNumber:x.supervisionPhase);
    return sortDir==="asc"?val(a).localeCompare(val(b)):val(b).localeCompare(val(a));
  });

  const saveClient=(c:Client)=>{
    const ccNumber = c.ccNumber || c.criminalCaseNumber;
    c.ccNumber = ccNumber;
    c.criminalCaseNumber = ccNumber;
    fetch(import.meta.env.BASE_URL + 'api/save_client.php', {
      method: 'POST',
      credentials: 'include',
      body: JSON.stringify(c),
      headers: {'Content-Type': 'application/json'}
    })
    .then(async (res) => {
      const text = await res.text();
      try {
        return JSON.parse(text);
      } catch (e) {
        throw new Error("Invalid server response: " + text.substring(0, 100));
      }
    })
    .then(res=>{
      if(res.success){
        const newClient = {...c, clientId: res.client_id || c.clientId};
        setClients(prev=>{const idx=prev.findIndex(x=>x.clientId===newClient.clientId);return idx>=0?prev.map(x=>x.clientId===newClient.clientId?newClient:x):[...prev,newClient];});
        setShowForm(false);setEditClient(undefined);
      } else { alert("Failed to save: " + res.message); }
    }).catch(err => {
      alert("Client Registration Error: " + err.message);
      console.error(err);
    });
  };
  const deleteClient=(id:string)=>{
    fetch(import.meta.env.BASE_URL + 'api/delete_client.php', { method: 'POST', credentials: 'include', body: JSON.stringify({clientId: id}) })
      .then(res=>res.json()).then(res=>{
        if(res.success){ setClients(p=>p.filter(c=>c.clientId!==id)); setDeleteId(null); }
      });
  };
  const deleteAllInactive=()=>{
    const inactive = clients.filter(c=>c.status!=="Active");
    Promise.all(inactive.map(c=>fetch(import.meta.env.BASE_URL + 'api/delete_client.php', { method: 'POST', credentials: 'include', body: JSON.stringify({clientId: c.clientId}) })))
      .then(() => { setClients(p=>p.filter(c=>c.status==="Active")); setDeleteInactiveConfirm(false); });
  };
  const markStatus=(id:string,st:Client["status"])=>{
    const c = clients.find(x=>x.clientId===id);
    if(c){
      const nc = {...c, status: st};
      fetch(import.meta.env.BASE_URL + 'api/save_client.php', { method: 'POST', credentials: 'include', body: JSON.stringify(nc) })
        .then(res=>res.json()).then(res=>{
          if(res.success) setClients(p=>p.map(x=>x.clientId===id?nc:x));
        });
    }
  };
  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      <Topbar title="Client Management" sub="CRUD — add, edit, and manage all client records" />
      <div className="flex-1 overflow-y-auto p-5 space-y-4">
        {endedActive.length>0&&(
          <div className="flex items-start gap-3 px-4 py-3 rounded border bg-red-50 border-red-200 text-xs text-red-800">
            <div className="w-2.5 h-2.5 rounded-full bg-red-500 mt-0.5 flex-shrink-0" />
            <div><strong>{endedActive.length}</strong> client{endedActive.length!==1?"s":""} with <strong>ended supervision period</strong>: {endedActive.map(c=>formatName(c)).join(", ")}.</div>
          </div>
        )}
        {nearExpiryActive.length>0&&(
          <div className="flex items-start gap-3 px-4 py-3 rounded border bg-amber-50 border-amber-200 text-xs text-amber-800">
            <div className="w-2 h-2 rounded-full bg-amber-400 mt-1 flex-shrink-0" />
            <div><strong>{nearExpiryActive.length}</strong> client{nearExpiryActive.length!==1?"s":""} with supervision ending within <strong>30 days</strong>: {nearExpiryActive.map(c=>formatName(c)).join(", ")}.</div>
          </div>
        )}

        <div className="bg-card rounded border border-border p-4 flex flex-wrap gap-3 items-center">
          <div className="flex-1 min-w-48 relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Search name, docket#, CC no…"
              className="w-full pl-8 pr-3 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none font-mono" />
          </div>
          {[
            {l:"Category",v:fCat,set:setFCat,opts:["All","Probationer","Parolee","Pardonee"]},
            {l:"Officer",v:fOfficer,set:setFOfficer,opts:["All",...OFFICERS]},
            {l:"Gender",v:fGender,set:setFGender,opts:["All","Male","Female"]},
            {l:"Remarks",v:fRemarks,set:setFRemarks,opts:["All","With Remarks","Without Remarks"]},
          ].map(({l,v,set,opts})=>(
            <select key={l} value={v} onChange={e=>set(e.target.value)} title={l}
              className="px-2 py-2 text-xs rounded border border-border bg-input-background text-foreground focus:outline-none font-mono">
              {opts.map(o=><option key={o}>{o}</option>)}
            </select>
          ))}
          <SortBar label="Sort:"
            options={[["docket","Docket"],["name","Name"],["case","CC No."],["phase","Phase"]] as ["docket"|"name"|"case"|"phase",string][]}
            sortBy={sortBy} sortDir={sortDir} onToggle={toggleSort}
          />
          <label className="flex items-center gap-1.5 cursor-pointer">
            <input type="checkbox" checked={showInactive} onChange={e=>setShowInactive(e.target.checked)} className="rounded" />
            <span className="text-[11px] text-muted-foreground font-semibold">Inactive ({inactiveCount})</span>
          </label>
          {inactiveCount>0&&(
            <button onClick={()=>setDeleteInactiveConfirm(true)} className="flex items-center gap-1.5 px-3 py-2 text-xs font-semibold rounded border border-red-200 text-red-600 bg-red-50 hover:bg-red-100">
              <Trash2 className="w-3.5 h-3.5" />Delete Inactive ({inactiveCount})
            </button>
          )}
          <button onClick={()=>{setEditClient(undefined);setShowForm(true);}} className="flex items-center gap-1.5 px-4 py-2 text-xs font-bold text-white rounded hover:opacity-90" style={{background:"var(--primary)"}}>
            <Plus className="w-3.5 h-3.5" />New Client
          </button>
        </div>

        <div className="bg-card rounded border border-border">
          <div className="px-5 py-3 border-b border-border flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="text-base font-bold text-foreground">{filtered.length}</span>
              <span className="text-xs text-muted-foreground">records</span>
              <span className="w-px h-4 bg-border" />
              <span className="text-sm font-bold text-blue-600">{filtered.filter(c=>c.gender==="Male").length}M</span>
              <span className="text-xs text-muted-foreground">/</span>
              <span className="text-sm font-bold text-pink-600">{filtered.filter(c=>c.gender==="Female").length}F</span>
            </div>
            <span className="text-[10px] text-muted-foreground font-mono">Double-click to view full profile</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead><tr className="border-b border-border bg-muted/30">
                {["Docket #","Full Name","Category","Gender","CC Number","Officer","Sup. End","Sup. Status","Remarks","Actions"].map(h=>(
                  <th key={h} className="text-left px-4 py-2.5 text-[10px] font-bold text-muted-foreground uppercase tracking-wide whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody className="divide-y divide-border">
                {filtered.map(c=>{
                  const st=supStatus(c);
                  const hasRemarks=!!(c.remarks||c.finalReport||c.violationReport);
                  return (
                    <tr key={c.clientId} onDoubleClick={()=>setViewClient(c)}
                      className={`cursor-pointer hover:bg-muted/20 transition-colors ${c.status!=="Active"?"opacity-60":""}`}>
                      <td className="px-4 py-2.5 font-mono text-sky-700">{c.docketNumber}</td>
                      <td className="px-4 py-2.5 font-semibold whitespace-nowrap">{formatName(c)}</td>
                      <td className="px-4 py-2.5"><Badge status={c.clientCategory} /></td>
                      <td className="px-4 py-2.5"><Badge status={c.gender} /></td>
                      <td className="px-4 py-2.5 font-mono text-muted-foreground">{c.ccNumber||"—"}</td>
                      <td className="px-4 py-2.5 text-muted-foreground whitespace-nowrap">{c.assignedOfficer}</td>
                      <td className="px-4 py-2.5 font-mono whitespace-nowrap">{c.supervisionEnd}</td>
                      <td className="px-4 py-2.5"><SupBadge client={c} /></td>
                      <td className="px-4 py-2.5">
                        {hasRemarks
                          ? <span className="text-[10px] text-emerald-700 font-mono">✓</span>
                          : <span className="text-[10px] text-red-400 font-mono italic">Missing</span>}
                      </td>
                      <td className="px-4 py-2.5">
                        <div className="flex items-center gap-1">
                          <button onClick={()=>setViewClient(c)} className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-sky-600"><Eye className="w-3.5 h-3.5" /></button>
                          <button onClick={()=>{setEditClient(c);setShowForm(true);}} className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-emerald-600"><Edit2 className="w-3.5 h-3.5" /></button>
                          {c.status==="Active"&&st==="Ended"&&<button onClick={()=>markStatus(c.clientId,"Completed")} title="Mark as Completed (supervision ended)" className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"><XCircle className="w-3.5 h-3.5" /></button>}
                          <button onClick={()=>setDeleteId(c.clientId)} className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"><Trash2 className="w-3.5 h-3.5" /></button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {filtered.length===0&&<tr><td colSpan={10} className="px-4 py-8 text-center text-muted-foreground font-mono text-[11px]">No records match.</td></tr>}
              </tbody>
            </table>
          </div>
        </div>
      </div>
      {showForm&&<ClientFormModal client={editClient} onSave={saveClient} onClose={()=>{setShowForm(false);setEditClient(undefined);}} nextId={nextId} />}
      {viewClient&&<ClientInfoModal client={viewClient} recs={recs} onClose={()=>setViewClient(null)} onEdit={()=>{setEditClient(viewClient);setViewClient(null);setShowForm(true);}} updateRec={updateRec} addRec={addRec} />}
      {deleteId&&(
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-lg border border-border p-6 w-full max-w-sm">
            <h3 className="text-sm font-bold text-foreground mb-2">Delete Client Record?</h3>
            <p className="text-xs text-muted-foreground mb-5">Permanently removes <span className="font-mono text-foreground">{deleteId}</span>. This cannot be undone.</p>
            <div className="flex gap-2">
              <button onClick={()=>setDeleteId(null)} className="flex-1 py-2 rounded border border-border text-xs font-semibold hover:bg-muted">Cancel</button>
              <button onClick={()=>deleteClient(deleteId)} className="flex-1 py-2 rounded text-xs font-bold text-white bg-red-600 hover:bg-red-700">Delete</button>
            </div>
          </div>
        </div>
      )}
      {deleteInactiveConfirm&&(
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-lg border border-border p-6 w-full max-w-sm">
            <h3 className="text-sm font-bold text-foreground mb-2">Delete All Inactive / Completed Records?</h3>
            <p className="text-xs text-muted-foreground mb-5">This will permanently delete <strong>{inactiveCount} record{inactiveCount!==1?"s":""}</strong>. Historical attendance data will be preserved.</p>
            <div className="flex gap-2">
              <button onClick={()=>setDeleteInactiveConfirm(false)} className="flex-1 py-2 rounded border border-border text-xs font-semibold hover:bg-muted">Cancel</button>
              <button onClick={deleteAllInactive} className="flex-1 py-2 rounded text-xs font-bold text-white bg-red-600 hover:bg-red-700">Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// APP ROOT
// ══════════════════════════════════════════════════════════════════════════════
export default function App() {
  const [page,setPage] = useState<Page>("login");
  const [clients,setClients] = useState<Client[]>([]);
  const [recs,setRecs] = useState<AttendanceRecord[]>([]);
  const [collapsed,setCollapsed] = useState(false);
  const [currentUser, setCurrentUser] = useState<any>(null);
  const [loadingSession, setLoadingSession] = useState(true);

  useEffect(() => {
    fetch(import.meta.env.BASE_URL + 'api/check_session.php', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
        if (data.success) {
          setCurrentUser(data.user);
          setPage("dashboard");
        }
        setLoadingSession(false);
      })
      .catch(() => setLoadingSession(false));

    const refreshData = () => {
      fetch(import.meta.env.BASE_URL + 'api/get_data.php', { credentials: 'include' })
        .then(res => res.json())
        .then(data => {
          if (data.success) {
            setClients(data.clients);
            setRecs(data.attendance);
          }
        })
        .catch(err => console.error(err));
    };

    window.addEventListener("refreshData", refreshData);
    refreshData();
    return () => window.removeEventListener("refreshData", refreshData);
  }, []);

  const addRec=(r:AttendanceRecord)=>{
    setRecs(p=>[r,...p]);
    fetch(import.meta.env.BASE_URL + 'api/add_attendance.php', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      credentials: 'include',
      body: JSON.stringify(r)
    }).catch(err => console.error(err));
  };

  const updateRec=(id:string, status:string)=>{
    setRecs(p=>p.map(r=>r.attendanceId===id ? {...r, status} : r));
    fetch(import.meta.env.BASE_URL + 'api/update_attendance.php', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      credentials: 'include',
      body: JSON.stringify({attendanceId: id, status})
    }).catch(err => console.error(err));
  };

  if (loadingSession) return <div className="flex h-screen items-center justify-center bg-background"><p className="text-muted-foreground text-sm font-semibold">Loading AMS...</p></div>;

  if (page==="login") return (
    <div style={{fontFamily:"'Plus Jakarta Sans',sans-serif"}}>
      <LoginPage onLogin={(u)=>{setCurrentUser(u);setPage("dashboard");}} />
    </div>
  );

  return (
    <div className="flex h-screen overflow-hidden bg-background" style={{fontFamily:"'Plus Jakarta Sans',sans-serif"}}>
      <Sidebar page={page} setPage={setPage} onLogout={()=>{
        fetch(import.meta.env.BASE_URL + 'api/logout.php', {credentials: 'include'}).then(()=>{
          setCurrentUser(null);
          setPage("login");
        });
      }} collapsed={collapsed} setCollapsed={setCollapsed} />
      <main className="flex-1 flex flex-col overflow-hidden">
        {page==="dashboard"  && <DashboardPage  clients={clients} recs={recs} setPage={setPage} />}
        {page==="attendance" && <AttendancePage clients={clients} recs={recs} addRec={addRec} updateRec={updateRec} />}
        {page==="history"    && <HistoryPage    clients={clients} recs={recs} updateRec={updateRec} addRec={addRec} />}
        {page==="search"     && <SearchPage     clients={clients} recs={recs} updateRec={updateRec} addRec={addRec} />}
        {page==="reports"    && <ReportsPage    clients={clients} recs={recs} updateRec={updateRec} addRec={addRec} />}
        {page==="management" && <ManagementPage clients={clients} setClients={setClients} recs={recs} updateRec={updateRec} addRec={addRec} />}
      </main>
    </div>
  );
}
