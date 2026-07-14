<?php
require_once 'db.php';

$data = json_decode(file_get_contents("php://input"), true);
$username = trim($data['username'] ?? '');
$password = trim($data['password'] ?? '');

if (empty($username) || empty($password)) {
    echo json_encode(["success" => false, "message" => "Please enter both username and password."]);
    exit;
}

try {
    $stmt = $pdo->prepare("SELECT user_id, username, password_hash, full_name, status FROM users WHERE username = ?");
    $stmt->execute([$username]);
    $user = $stmt->fetch(PDO::FETCH_ASSOC);

    if ($user) {
        if ($user['status'] !== 'Active') {
            echo json_encode(["success" => false, "message" => "Account is inactive. Please contact administrator."]);
            exit;
        }

        if (password_verify($password, $user['password_hash'])) {
            // Password is correct, start session
            $_SESSION['user_id'] = $user['user_id'];
            $_SESSION['username'] = $user['username'];
            $_SESSION['full_name'] = $user['full_name'];
            
            echo json_encode([
                "success" => true,
                "user" => [
                    "userId" => $user['user_id'],
                    "username" => $user['username'],
                    "fullName" => $user['full_name']
                ]
            ]);
        } else {
            echo json_encode(["success" => false, "message" => "Invalid username or password."]);
        }
    } else {
        echo json_encode(["success" => false, "message" => "Invalid username or password."]);
    }
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Database error: " . $e->getMessage()]);
}
?>
