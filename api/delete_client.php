<?php
require_once 'db.php';

$data = json_decode(file_get_contents("php://input"), true);
if (!$data || empty($data['clientId'])) {
    die(json_encode(["success" => false, "message" => "Invalid payload"]));
}

$client_id = $data['clientId'];

try {
    $stmt = $pdo->prepare("DELETE FROM clients WHERE client_id = ?");
    $stmt->execute([$client_id]);
    echo json_encode(["success" => true]);
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Database error: " . $e->getMessage()]);
}
?>
