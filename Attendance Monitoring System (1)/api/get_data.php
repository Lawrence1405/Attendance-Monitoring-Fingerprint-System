<?php
require_once 'db.php';

try {
    $clients = [];
    $stmt = $pdo->query("SELECT * FROM clients");
    while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
        // Map database fields to frontend structure
        $client = [
            "clientId" => $row['client_id'],
            "docketNumber" => $row['docket_number'] ?? "",
            "fullName" => $row['full_name'] ?? "",
            "middleInitial" => $row['middle_initial'] ?? "",
            "gender" => $row['gender'] ?? "Male",
            "clientCategory" => $row['client_category'] ?? "Probationer",
            "ccNumber" => $row['criminal_case_number'] ?? "",
            "criminalCaseNumber" => $row['criminal_case_number'] ?? "",
            "court" => $row['court'] ?? "",
            "doNdo" => $row['do_ndo'] ?? "DO",
            "assignedOfficer" => $row['assigned_officer'] ?? "",
            "supervisionStart" => $row['supervision_start'] ?? "",
            "supervisionEnd" => $row['supervision_end'] ?? "",
            "supervisionPhase" => $row['supervision_phase'] ?? "Phase 1",
            "registrationDate" => $row['registration_date'] ?? "",
            "status" => $row['status'] ?? "Active",
            "caseType" => $row['case_type'] ?? "Non-Drug",
            "remarks" => $row['remarks'] ?? "",
            "finalReport" => $row['final_report'] ?? "",
            "finalReportDate" => $row['final_report_date'] ?? "",
            "terminationDate" => $row['termination_date'] ?? "",
            "violationReport" => $row['violation_report'] ?? "",
            "violationDate" => $row['violation_date'] ?? "",
            "courtOrderDisposition" => $row['court_order_disposition'] ?? "",
            "courtOrderDateSubmitted" => $row['court_order_date_submitted'] ?? "",
            "courtOrderDateReceived" => $row['court_order_date_received'] ?? "",
            "fingerprintId" => $row['fingerprint_id'] ?? "",
            "fingerprintEnrolled" => (bool)$row['fingerprint_enrolled'],
            "fingerprintEnrollmentDate" => $row['fingerprint_enrollment_date'] ?? "",
            "fingerprintImage" => $row['fingerprint_image'] ?? "",
            "piNumber" => $row['pi_number'] ?? "",
            "alias" => $row['alias'] ?? "",
            "identifyingMarks" => $row['identifying_marks'] ?? "",
            "address" => $row['address'] ?? "",
            "barangay" => $row['barangay'] ?? "",
            "contactNumber" => $row['contact_number'] ?? "",
            "dateOfBirth" => $row['date_of_birth'] ?? "",
            "placeOfBirth" => $row['place_of_birth'] ?? "",
            "civilStatus" => $row['civil_status'] ?? "Single",
            "spouseName" => $row['spouse_name'] ?? "",
            "numberOfDependents" => (string)$row['number_of_dependents'],
            "educationalAttainment" => $row['educational_attainment'] ?? "",
            "occupation" => $row['occupation'] ?? "",
            "monthlyIncome" => $row['monthly_income'] ? (string)floatval($row['monthly_income']) : "",
            "hobbies" => $row['hobbies'] ?? "",
            "skills" => $row['skills'] ?? "",
            "religiousAffiliation" => $row['religious_affiliation'] ?? "",
            "psiNumber" => $row['psi_number'] ?? "",
            "chargedWith" => $row['charged_with'] ?? "",
            "dateCommitted" => $row['date_committed'] ?? "",
            "convictedOf" => $row['convicted_of'] ?? "",
            "dateConvicted" => $row['date_convicted'] ?? "",
            "sentence" => $row['sentence'] ?? "",
            "placeOfReferral" => $row['place_of_referral'] ?? "",
            "datePsiSubmitted" => $row['date_psi_submitted'] ?? "",
            "custodyStatus" => $row['custody_status'] ?? "",
            "dateProbationGranted" => $row['date_probation_granted'] ?? "",
            "dateProbationOrderReceived" => $row['date_probation_order_received'] ?? "",
            "periodOfProbation" => $row['period_of_probation'] ?? "",
            "dateFrSubmitted" => $row['date_fr_submitted'] ?? "",
            "dateOfToro" => $row['date_of_toro'] ?? "",
            "dateReceivedCase" => $row['date_received_case'] ?? "",
            "investigatingOfficer" => $row['investigating_officer'] ?? "",
            "photoInitial" => $row['full_name'] ? strtoupper(substr(trim($row['full_name']), 0, 1)) : "U"
        ];
        $clients[] = $client;
    }

    $attendance = [];
    $stmt_att = $pdo->query("
        SELECT a.*, c.full_name, c.criminal_case_number, c.docket_number 
        FROM attendance_records a
        LEFT JOIN clients c ON a.client_id = c.client_id
    ");
    while ($row = $stmt_att->fetch(PDO::FETCH_ASSOC)) {
        $attendance[] = [
            "attendanceId" => $row['attendance_id'],
            "clientId" => $row['client_id'],
            "fullName" => $row['full_name'] ?? "",
            "caseNumber" => $row['criminal_case_number'] ?? "",
            "docketNumber" => $row['docket_number'] ?? "",
            "date" => $row['record_date'],
            "time" => substr($row['record_time'], 0, 5), // Format HH:MM
            "verifiedBy" => $row['verified_by'] ?? "",
            "status" => $row['status'] ?? "Present"
        ];
    }

    echo json_encode(["success" => true, "clients" => $clients, "attendance" => $attendance]);
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Failed to fetch data: " . $e->getMessage()]);
}
?>
