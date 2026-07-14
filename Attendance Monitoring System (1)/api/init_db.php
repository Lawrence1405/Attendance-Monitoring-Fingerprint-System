<?php
require_once 'db.php';

try {
    // Create database if not exists
    $pdo->exec("CREATE DATABASE IF NOT EXISTS ams_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
    $pdo->exec("USE ams_db");

    // Create clients table
    $sql_clients = "CREATE TABLE IF NOT EXISTS clients (
        client_id VARCHAR(50) PRIMARY KEY,
        docket_number VARCHAR(100),
        full_name VARCHAR(255),
        middle_initial VARCHAR(10),
        gender VARCHAR(50),
        client_category VARCHAR(50),
        criminal_case_number VARCHAR(100),
        court VARCHAR(255),
        do_ndo VARCHAR(10),
        assigned_officer VARCHAR(255),
        supervision_start DATE NULL,
        supervision_end DATE NULL,
        supervision_phase VARCHAR(50),
        registration_date DATE NULL,
        status VARCHAR(50),
        case_type VARCHAR(50),
        remarks TEXT,
        final_report VARCHAR(255),
        final_report_date DATE NULL,
        termination_date DATE NULL,
        violation_report VARCHAR(255),
        violation_date DATE NULL,
        court_order_disposition VARCHAR(255),
        court_order_date_submitted DATE NULL,
        court_order_date_received DATE NULL,
        fingerprint_id VARCHAR(50),
        fingerprint_template LONGTEXT,
        fingerprint_image LONGTEXT,
        fingerprint_enrolled TINYINT(1) DEFAULT 0,
        fingerprint_enrollment_date DATE NULL,
        pi_number VARCHAR(100),
        alias VARCHAR(255),
        identifying_marks VARCHAR(255),
        address TEXT,
        barangay VARCHAR(255),
        contact_number VARCHAR(50),
        date_of_birth DATE NULL,
        place_of_birth VARCHAR(255),
        civil_status VARCHAR(50),
        spouse_name VARCHAR(255),
        number_of_dependents INT DEFAULT 0,
        educational_attainment VARCHAR(255),
        occupation VARCHAR(255),
        monthly_income DECIMAL(10,2) NULL,
        hobbies TEXT,
        skills TEXT,
        religious_affiliation VARCHAR(255),
        psi_number VARCHAR(100),
        charged_with VARCHAR(255),
        date_committed DATE NULL,
        convicted_of VARCHAR(255),
        date_convicted DATE NULL,
        sentence VARCHAR(255),
        place_of_referral VARCHAR(255),
        date_psi_submitted DATE NULL,
        custody_status VARCHAR(50),
        date_probation_granted DATE NULL,
        date_probation_order_received DATE NULL,
        period_of_probation VARCHAR(100),
        date_fr_submitted DATE NULL,
        date_of_toro DATE NULL,
        date_received_case DATE NULL,
        investigating_officer VARCHAR(255)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";
    $pdo->exec($sql_clients);

    // Create attendance_records table
    $sql_attendance = "CREATE TABLE IF NOT EXISTS attendance_records (
        attendance_id VARCHAR(100) PRIMARY KEY,
        client_id VARCHAR(50),
        record_date DATE NOT NULL,
        record_time TIME NOT NULL,
        verified_by VARCHAR(255),
        status VARCHAR(50) DEFAULT 'Present',
        FOREIGN KEY (client_id) REFERENCES clients(client_id) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";
    $pdo->exec($sql_attendance);

    // Create users table
    $sql_users = "CREATE TABLE IF NOT EXISTS users (
        user_id INT AUTO_INCREMENT PRIMARY KEY,
        username VARCHAR(50) NOT NULL UNIQUE,
        password_hash VARCHAR(255) NOT NULL,
        full_name VARCHAR(255) NOT NULL,
        status VARCHAR(50) DEFAULT 'Active',
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";
    $pdo->exec($sql_users);

    // Seed default admin user if no users exist
    $stmt = $pdo->query("SELECT COUNT(*) FROM users");
    if ($stmt->fetchColumn() == 0) {
        $admin_password_hash = password_hash('admin123', PASSWORD_BCRYPT);
        $stmt = $pdo->prepare("INSERT INTO users (username, password_hash, full_name, status) VALUES (?, ?, ?, ?)");
        $stmt->execute(['admin', $admin_password_hash, 'System Administrator', 'Active']);
    }

    // Add status column to existing table if it doesn't exist
    try {
        $pdo->exec("ALTER TABLE attendance_records ADD COLUMN status VARCHAR(50) DEFAULT 'Present'");
    } catch (PDOException $e) {
        // column likely exists, ignore
    }

    // Add fingerprint_image column to existing clients table if it doesn't exist
    try {
        $pdo->exec("ALTER TABLE clients ADD COLUMN fingerprint_image LONGTEXT");
    } catch (PDOException $e) {
        // column likely exists, ignore
    }

    echo json_encode(["success" => true, "message" => "Database and tables initialized successfully."]);
} catch (PDOException $e) {
    echo json_encode(["success" => false, "message" => "Initialization failed: " . $e->getMessage()]);
}
?>
