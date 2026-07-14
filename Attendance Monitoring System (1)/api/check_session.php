<?php
require_once 'db.php';

if (isset($_SESSION['user_id'])) {
    echo json_encode([
        "success" => true,
        "user" => [
            "userId" => $_SESSION['user_id'],
            "username" => $_SESSION['username'],
            "fullName" => $_SESSION['full_name']
        ]
    ]);
} else {
    echo json_encode(["success" => false, "message" => "Not logged in"]);
}
?>
