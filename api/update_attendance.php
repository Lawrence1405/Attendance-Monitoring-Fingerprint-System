<?php
require_once 'db.php';

$data = json_decode(file_get_contents("php://input"), true);
if (!$data || empty($data['attendanceId'])) {
    die(json_encode(["success" => false, "message" => "Invalid payload"]));
}

$attendance_id = $data['attendanceId'];
$status = $data['status'] ?? "Present";

try {
    $stmt = $pdo->prepare("UPDATE attendance_records SET status = ? WHERE attendance_id = ?");
    $stmt->execute([$status, $attendance_id]);
    echo json_encode(["success" => true]);
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Database error: " . $e->getMessage()]);
}
?>
