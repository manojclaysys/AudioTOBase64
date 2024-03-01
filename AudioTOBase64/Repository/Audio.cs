
using AudioTOBase64.Models;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace AudioTOBase64.Repository
{
    public class Audio
    {
        // Audio to base64
        private readonly IConfiguration _configuration;
        private SqlConnection connection;

        public Audio(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Connect()
        {

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            connection = new SqlConnection(connectionString);
        }
        public async Task<string> PostAudio(Class model)
        {

            using (MemoryStream memoryStream = new MemoryStream())
            {
              //  await model.File.CopyToAsync(memoryStream);

              //  string base64String = Convert.ToBase64String(memoryStream.ToArray());

              //  string audioString = null;
                return await PostToExternalApi(model.File, model);


            }
        }
        public string covertString;
        public async Task<string> PostToExternalApi(string base64String, Class model)
        {
            model.Email = model.Email ?? "";
            model.EmployeeID = (model.Email != null && model.Email != "") ? "" : model.EmployeeID;
            string externalApiUrl = _configuration.GetSection("APIConnection")["URL"];
          //  string encodedString = Uri.EscapeDataString(base64String);
            var payload = new
            {
                model.EmployeeID,
                model.Email,
               model.File,
                model.Prompts,
            };

            try
            {

                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(externalApiUrl, content);
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var deserializedResponse = JsonSerializer.Deserialize<dynamic>(jsonResponse);

                    // Extract the Result value from the deserialized response
                    //string resultValue = deserializedResponse.Result;


                    if (deserializedResponse.TryGetProperty("Result", out JsonElement resultProperty))
                    {
                        string resultValue = resultProperty.GetString();

                        // Use the "resultValue" variable as needed
                        if (resultValue == "Live")
                        {
                            return "Success";
                        }
                        else
                        {
                            return "Fail";
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Response does not contain 'Result' property.");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Fail");
            }
        }
        public string GetRandomPromptsFromDatabase()
        {
            Connect();

            string arrayvalue = "";

            connection.Open();
            string query = "SELECT TOP (@Count) * FROM AudioPrompts WHERE IsActivePrompt = 1 ORDER BY NEWID()";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Count", 1);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        arrayvalue = reader["Prompt"].ToString();

                    }
                }
            }

            connection.Close(); // Close the connection after usage

            return arrayvalue;
        }



    }
}
