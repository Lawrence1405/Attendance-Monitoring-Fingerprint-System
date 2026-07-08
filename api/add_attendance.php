<?php
require_once 'db.php';

$data = json_decode(file_get_contents("php://input"), true);
if (!$data || empty($data['clientId'])) {
    die(json_encode(["success" => false, "message" => "Invalid payload"]));
}

$attendance_id = $data['attendanceId'] ?? uniqid("ATT-");
$client_id = $data['clientId'];
$date = $data['date'] ?? date('Y-m-d');
$time = $data['time'] ?? date('H:i');
$verified_by = $data['verifiedBy'] ?? "";
$status = $data['status'] ?? "Present";

try {
    $stmt = $pdo->prepare("INSERT INTO attendance_records (attendance_id, client_id, record_date, record_time, verified_by, status) VALUES (?, ?, ?, ?, ?, ?)");
    $stmt->execute([$attendance_id, $client_id, $date, $time, $verified_by, $status]);
    echo json_encode(["success" => true, "attendanceId" => $attendance_id]);
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Database error: " . $e->getMessage()]);
}
?>
