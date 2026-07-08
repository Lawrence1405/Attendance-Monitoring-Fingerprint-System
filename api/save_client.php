<?php
require_once 'db.php';

$data = json_decode(file_get_contents("php://input"), true);
if (!$data) {
    die(json_encode(["success" => false, "message" => "Invalid JSON payload"]));
}

// Data consistency rule: "Court Criminal Case (CC) Number" and "Case Profile -> Court Criminal Case Number"
// Map them to a single field. If one is empty and the other is set, use the set one.
$criminal_case_number = $data['ccNumber'] ?? "";
if (empty($criminal_case_number) && !empty($data['criminalCaseNumber'])) {
    $criminal_case_number = $data['criminalCaseNumber'];
}

$client_id = $data['clientId'] ?? uniqid("TPP-");

$fields = [
    'client_id' => $client_id,
    'docket_number' => $data['docketNumber'] ?? null,
    'full_name' => $data['fullName'] ?? null,
    'middle_initial' => $data['middleInitial'] ?? null,
    'gender' => $data['gender'] ?? null,
    'client_category' => $data['clientCategory'] ?? null,
    'criminal_case_number' => $criminal_case_number,
    'court' => $data['court'] ?? null,
    'do_ndo' => $data['doNdo'] ?? null,
    'assigned_officer' => $data['assignedOfficer'] ?? null,
    'supervision_start' => empty($data['supervisionStart']) ? null : $data['supervisionStart'],
    'supervision_end' => empty($data['supervisionEnd']) ? null : $data['supervisionEnd'],
    'supervision_phase' => $data['supervisionPhase'] ?? null,
    'registration_date' => empty($data['registrationDate']) ? null : $data['registrationDate'],
    'status' => $data['status'] ?? null,
    'case_type' => $data['caseType'] ?? null,
    'remarks' => $data['remarks'] ?? null,
    'final_report' => $data['finalReport'] ?? null,
    'final_report_date' => empty($data['finalReportDate']) ? null : $data['finalReportDate'],
    'termination_date' => empty($data['terminationDate']) ? null : $data['terminationDate'],
    'violation_report' => $data['violationReport'] ?? null,
    'violation_date' => empty($data['violationDate']) ? null : $data['violationDate'],
    'court_order_disposition' => $data['courtOrderDisposition'] ?? null,
    'court_order_date_submitted' => empty($data['courtOrderDateSubmitted']) ? null : $data['courtOrderDateSubmitted'],
    'court_order_date_received' => empty($data['courtOrderDateReceived']) ? null : $data['courtOrderDateReceived'],
    'fingerprint_id' => $data['fingerprintId'] ?? null,
    'fingerprint_enrolled' => !empty($data['fingerprintEnrolled']) ? 1 : 0,
    'fingerprint_enrollment_date' => empty($data['fingerprintEnrollmentDate']) ? null : $data['fingerprintEnrollmentDate'],
    'pi_number' => $data['piNumber'] ?? null,
    'alias' => $data['alias'] ?? null,
    'identifying_marks' => $data['identifyingMarks'] ?? null,
    'address' => $data['address'] ?? null,
    'barangay' => $data['barangay'] ?? null,
    'contact_number' => $data['contactNumber'] ?? null,
    'date_of_birth' => empty($data['dateOfBirth']) ? null : $data['dateOfBirth'],
    'place_of_birth' => $data['placeOfBirth'] ?? null,
    'civil_status' => $data['civilStatus'] ?? null,
    'spouse_name' => $data['spouseName'] ?? null,
    'number_of_dependents' => isset($data['numberOfDependents']) ? (int)$data['numberOfDependents'] : 0,
    'educational_attainment' => $data['educationalAttainment'] ?? null,
    'occupation' => $data['occupation'] ?? null,
    'monthly_income' => !empty($data['monthlyIncome']) ? (float)$data['monthlyIncome'] : null,
    'hobbies' => $data['hobbies'] ?? null,
    'skills' => $data['skills'] ?? null,
    'religious_affiliation' => $data['religiousAffiliation'] ?? null,
    'psi_number' => $data['psiNumber'] ?? null,
    'charged_with' => $data['chargedWith'] ?? null,
    'date_committed' => empty($data['dateCommitted']) ? null : $data['dateCommitted'],
    'convicted_of' => $data['convictedOf'] ?? null,
    'date_convicted' => empty($data['dateConvicted']) ? null : $data['dateConvicted'],
    'sentence' => $data['sentence'] ?? null,
    'place_of_referral' => $data['placeOfReferral'] ?? null,
    'date_psi_submitted' => empty($data['datePsiSubmitted']) ? null : $data['datePsiSubmitted'],
    'custody_status' => $data['custodyStatus'] ?? null,
    'date_probation_granted' => empty($data['dateProbationGranted']) ? null : $data['dateProbationGranted'],
    'date_probation_order_received' => empty($data['dateProbationOrderReceived']) ? null : $data['dateProbationOrderReceived'],
    'period_of_probation' => $data['periodOfProbation'] ?? null,
    'date_fr_submitted' => empty($data['dateFrSubmitted']) ? null : $data['dateFrSubmitted'],
    'date_of_toro' => empty($data['dateOfToro']) ? null : $data['dateOfToro'],
    'date_received_case' => empty($data['dateReceivedCase']) ? null : $data['dateReceivedCase'],
    'investigating_officer' => $data['investigatingOfficer'] ?? null,
];

try {
    // Check if client exists
    $stmt = $pdo->prepare("SELECT client_id FROM clients WHERE client_id = ?");
    $stmt->execute([$client_id]);
    $exists = $stmt->fetch();

    if ($exists) {
        $updateParts = [];
        $params = [];
        foreach ($fields as $key => $value) {
            if ($key !== 'client_id') {
                $updateParts[] = "$key = ?";
                $params[] = $value;
            }
        }
        $params[] = $client_id;
        $sql = "UPDATE clients SET " . implode(", ", $updateParts) . " WHERE client_id = ?";
        $stmt = $pdo->prepare($sql);
        $stmt->execute($params);
    } else {
        $columns = implode(", ", array_keys($fields));
        $placeholders = implode(", ", array_fill(0, count($fields), "?"));
        $sql = "INSERT INTO clients ($columns) VALUES ($placeholders)";
        $stmt = $pdo->prepare($sql);
        $stmt->execute(array_values($fields));
    }

    echo json_encode(["success" => true, "client_id" => $client_id]);
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Database error: " . $e->getMessage()]);
}
?>
