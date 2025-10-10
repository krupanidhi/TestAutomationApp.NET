using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Services;

public interface ITestDataService
{
    Task<TestDataResponse> CreateTestDataSetAsync(TestDataRequest request);
    Task<TestDataResponse> GetTestDataSetAsync(string id);
    Task<TestDataListResponse> GetAllTestDataSetsAsync();
    Task<TestDataResponse> UpdateTestDataSetAsync(string id, TestDataRequest request);
    Task<bool> DeleteTestDataSetAsync(string id);
    Task<string> EncryptValue(string value);
    Task<string> DecryptValue(string encryptedValue);
}

public class TestDataService : ITestDataService
{
    private readonly ILogger<TestDataService> _logger;
    private readonly string _dataDirectory;
    private readonly byte[] _encryptionKey;

    public TestDataService(ILogger<TestDataService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        // Get encryption key from config or generate one
        var keyString = configuration["TestData:EncryptionKey"] ?? "TestAutomationApp-SecureKey-2024";
        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    public async Task<TestDataResponse> CreateTestDataSetAsync(TestDataRequest request)
    {
        try
        {
            var dataSet = new TestDataSet
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Environment = request.Environment,
                Data = new Dictionary<string, TestDataEntry>()
            };

            // Process and encrypt secure values
            foreach (var entry in request.Data)
            {
                var dataEntry = entry.Value;
                if (dataEntry.IsSecure && !string.IsNullOrEmpty(dataEntry.Value))
                {
                    dataEntry.Value = await EncryptValue(dataEntry.Value);
                }
                dataSet.Data[entry.Key] = dataEntry;
            }

            // Save to file
            var filePath = Path.Combine(_dataDirectory, $"{dataSet.Id}.json");
            var json = JsonSerializer.Serialize(dataSet, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Created test data set: {Name} ({Id})", dataSet.Name, dataSet.Id);

            return new TestDataResponse
            {
                Success = true,
                Message = "Test data set created successfully",
                Data = dataSet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test data set");
            return new TestDataResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<TestDataResponse> GetTestDataSetAsync(string id)
    {
        try
        {
            var filePath = Path.Combine(_dataDirectory, $"{id}.json");
            
            if (!File.Exists(filePath))
            {
                return new TestDataResponse
                {
                    Success = false,
                    Message = "Test data set not found"
                };
            }

            var json = await File.ReadAllTextAsync(filePath);
            var dataSet = JsonSerializer.Deserialize<TestDataSet>(json);

            if (dataSet == null)
            {
                return new TestDataResponse
                {
                    Success = false,
                    Message = "Failed to deserialize test data set"
                };
            }

            // Decrypt secure values for display (masked)
            foreach (var entry in dataSet.Data.Values.Where(e => e.IsSecure))
            {
                entry.Value = "********"; // Don't expose encrypted values
            }

            return new TestDataResponse
            {
                Success = true,
                Data = dataSet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test data set");
            return new TestDataResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<TestDataListResponse> GetAllTestDataSetsAsync()
    {
        try
        {
            var files = Directory.GetFiles(_dataDirectory, "*.json");
            var dataSets = new List<TestDataSet>();

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var dataSet = JsonSerializer.Deserialize<TestDataSet>(json);
                
                if (dataSet != null)
                {
                    // Mask secure values
                    foreach (var entry in dataSet.Data.Values.Where(e => e.IsSecure))
                    {
                        entry.Value = "********";
                    }
                    dataSets.Add(dataSet);
                }
            }

            return new TestDataListResponse
            {
                DataSets = dataSets.OrderByDescending(d => d.CreatedAt).ToList(),
                TotalCount = dataSets.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test data sets");
            return new TestDataListResponse();
        }
    }

    public async Task<TestDataResponse> UpdateTestDataSetAsync(string id, TestDataRequest request)
    {
        try
        {
            var filePath = Path.Combine(_dataDirectory, $"{id}.json");
            
            if (!File.Exists(filePath))
            {
                return new TestDataResponse
                {
                    Success = false,
                    Message = "Test data set not found"
                };
            }

            var json = await File.ReadAllTextAsync(filePath);
            var dataSet = JsonSerializer.Deserialize<TestDataSet>(json);

            if (dataSet == null)
            {
                return new TestDataResponse
                {
                    Success = false,
                    Message = "Failed to deserialize test data set"
                };
            }

            // Update properties
            dataSet.Name = request.Name;
            dataSet.Description = request.Description;
            dataSet.Environment = request.Environment;
            dataSet.UpdatedAt = DateTime.UtcNow;

            // Update data entries
            dataSet.Data = new Dictionary<string, TestDataEntry>();
            foreach (var entry in request.Data)
            {
                var dataEntry = entry.Value;
                if (dataEntry.IsSecure && !string.IsNullOrEmpty(dataEntry.Value) && dataEntry.Value != "********")
                {
                    dataEntry.Value = await EncryptValue(dataEntry.Value);
                }
                dataSet.Data[entry.Key] = dataEntry;
            }

            // Save
            json = JsonSerializer.Serialize(dataSet, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Updated test data set: {Name} ({Id})", dataSet.Name, dataSet.Id);

            return new TestDataResponse
            {
                Success = true,
                Message = "Test data set updated successfully",
                Data = dataSet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating test data set");
            return new TestDataResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<bool> DeleteTestDataSetAsync(string id)
    {
        try
        {
            var filePath = Path.Combine(_dataDirectory, $"{id}.json");
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted test data set: {Id}", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting test data set");
            return false;
        }
    }

    public async Task<string> EncryptValue(string value)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            
            // Write IV first
            await msEncrypt.WriteAsync(aes.IV);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                await swEncrypt.WriteAsync(value);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting value");
            return value; // Return original if encryption fails
        }
    }

    public async Task<string> DecryptValue(string encryptedValue)
    {
        try
        {
            var buffer = Convert.FromBase64String(encryptedValue);
            
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            // Extract IV
            var iv = new byte[aes.IV.Length];
            Array.Copy(buffer, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return await srDecrypt.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting value");
            return encryptedValue; // Return encrypted if decryption fails
        }
    }
}
