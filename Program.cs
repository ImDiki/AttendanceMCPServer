using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace AttendanceMCPServer
{
    class Program
    {
        // 
        static string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\mainlineDB.mdf;Integrated Security=True";

        static void Main(string[] args)
        {
            while (true)
            {
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) break;

                try
                {
                    using var doc = JsonDocument.Parse(input);
                    var root = doc.RootElement;
                    var method = root.GetProperty("method").GetString();

      
                    long id = 0;
                    if (root.TryGetProperty("id", out var idElem))
                    {
                        if (idElem.ValueKind == JsonValueKind.Number) id = idElem.GetInt64();
                    }

                    if (method == "initialize")
                    {
                        SendResponse(id, new
                        {
                            protocolVersion = "2025-06-18",
                            capabilities = new { tools = new { } },
                            serverInfo = new { name = "attendance-server", version = "1.0.0" }
                        });
                    }
                    else if (method == "tools/list")
                    {
                        var tools = new
                        {
                            tools = new[] {
                                new {
                                    name = "get_student_info",
                                    description = "Get student details by code from OIC Attendance Database",
                                    inputSchema = new {
                                        type = "object",
                                        properties = new { studentCode = new { type = "string" } },
                                        required = new[] { "studentCode" }
                                    }
                                }
                            }
                        };
                        SendResponse(id, tools);
                    }
                    else if (method == "tools/call") // Claude standard က tools/call ဖြစ်ပါတယ်
                    {
                        var toolName = root.GetProperty("params").GetProperty("name").GetString();
                        if (toolName == "get_student_info")
                        {
                            var studentCode = root.GetProperty("params").GetProperty("arguments").GetProperty("studentCode").GetString();
                            string result = FetchFromDB(studentCode);

                            // Claude က မျှော်လင့်တဲ့ tool result format ဖြစ်အောင် ပြင်လိုက်ပါတယ်
                            SendResponse(id, new
                            {
                                content = new[] { new { type = "text", text = result } },
                                isError = result.StartsWith("Database Error")
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[JSON ERROR] {ex.Message}");
                }
            }
        }

        static string FetchFromDB(string code)
        {
            try
            {
                Console.Error.WriteLine($"[LOG] Searching database for: {code}");
                using var conn = new SqlConnection(connectionString);

                // Connection ကို အမြန်ဖွင့်ပါမယ်
                conn.Open();

                string sql = "SELECT FullName, Class, YearLevel FROM Students WHERE StudentCode = @code";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 2; // Query execution ကို ၂ စက္ကန့်ပဲ စောင့်မယ်
                cmd.Parameters.AddWithValue("@code", code);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return $"[FOUND] Name: {reader["FullName"]}, Class: {reader["Class"]}, Year: {reader["YearLevel"]}";
                }
                return "Student not found in database.";
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"[DB ERROR] {ex.Message}");
                return $"Database Error: {ex.Message} (Check if Visual Studio has locked the DB file)";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GENERAL ERROR] {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        static void SendResponse(long id, object result)
        {
            var response = new { jsonrpc = "2.0", id, result };
            Console.WriteLine(JsonSerializer.Serialize(response));
        }
    }
}