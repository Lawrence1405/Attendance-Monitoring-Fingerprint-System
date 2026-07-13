using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace FPTester
{
    public class DatabaseTemplateStore
    {
        private readonly string _connectionString = "Server=127.0.0.1;Database=ams_db;User ID=root;Password=;";

        public Dictionary<string, byte[]> GetAllTemplates()
        {
            var templates = new Dictionary<string, byte[]>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT client_id, fingerprint_template FROM clients WHERE fingerprint_template IS NOT NULL";
                    
                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string clientId = reader.GetString("client_id");
                            string base64Template = reader.GetString("fingerprint_template");
                            
                            try
                            {
                                byte[] templateBytes = Convert.FromBase64String(base64Template);
                                templates[clientId] = templateBytes;
                            }
                            catch
                            {
                                // Ignore invalid base64 data
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching templates from DB: {ex.Message}");
            }

            return templates;
        }
        public bool SaveTemplate(string clientId, byte[] templateBytes)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string base64 = Convert.ToBase64String(templateBytes);
                    string query = "UPDATE clients SET fingerprint_template = @base64, fingerprint_enrolled = 1, fingerprint_enrollment_date = CURDATE() WHERE client_id = @clientId";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@base64", base64);
                        command.Parameters.AddWithValue("@clientId", clientId);
                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving template to DB: {ex.Message}");
                return false;
            }
        }

        public bool RemoveTemplate(string clientId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "UPDATE clients SET fingerprint_template = NULL, fingerprint_enrolled = 0, fingerprint_enrollment_date = NULL WHERE client_id = @clientId";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);
                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing template from DB: {ex.Message}");
                return false;
            }
        }
    }
}
