using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace AttendanceMCPServer
{
    class Program
    {
        private static readonly string ConnectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\mainlineDB.mdf;Integrated Security=True";

        static void Main(string[] args)
        {
            while (true)
            {
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }

                try
                {
                    using var doc = JsonDocument.Parse(input);
                    var root = doc.RootElement;
                    var method = root.GetProperty("method").GetString();

                    long id = 0;
                    if (root.TryGetProperty("id", out var idElement) &&
                        idElement.ValueKind == JsonValueKind.Number)
                    {
                        id = idElement.GetInt64();
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
                        SendResponse(id, new
                        {
                            tools = new[]
                            {
                                new
                                {
                                    name = "get_student_info",
                                    description = "Get student details by code from OIC Attendance Database",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new { studentCode = new { type = "string" } },
                                        required = new[] { "studentCode" }
                                    }
                                }
                            }
                        });
                    }
                    else if (method == "tools/call")
                    {
                        var parameters = root.GetProperty("params");
                        var toolName = parameters.GetProperty("name").GetString();

                        if (toolName == "get_student_info")
                        {
                            var studentCode = parameters
                                .GetProperty("arguments")
                                .GetProperty("studentCode")
                                .GetString();

                            string result = FetchFromDatabase(studentCode);

                            SendResponse(id, new
                            {
                                content = new[] { new { type = "text", text = result } },
                                isError = result.StartsWith("Database Error", StringComparison.Ordinal)
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

        private static string FetchFromDatabase(string? studentCode)
        {
            if (string.IsNullOrWhiteSpace(studentCode))
            {
                return "Student code is required.";
            }

            try
            {
                Console.Error.WriteLine($"[LOG] Searching database for: {studentCode}");
                using var connection = new SqlConnection(ConnectionString);
                connection.Open();

                const string sql =
                    "SELECT FullName, Class, YearLevel FROM Students WHERE StudentCode = @code";

                using var command = new SqlCommand(sql, connection)
                {
                    CommandTimeout = 2
                };
                command.Parameters.Add("@code", SqlDbType.NVarChar, 50).Value = studentCode;

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return $"[FOUND] Name: {reader["FullName"]}, Class: {reader["Class"]}, Year: {reader["YearLevel"]}";
                }

                return "Student not found in database.";
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"[DB ERROR] {ex.Message}");
                return $"Database Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GENERAL ERROR] {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private static void SendResponse(long id, object result)
        {
            var response = new { jsonrpc = "2.0", id, result };
            Console.WriteLine(JsonSerializer.Serialize(response));
        }
    }
}
