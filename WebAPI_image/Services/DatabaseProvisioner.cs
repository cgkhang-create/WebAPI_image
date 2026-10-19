using Microsoft.Data.SqlClient;

namespace WebAPI_image.Services
{
    public class DatabaseProvisioner : IDatabaseProvisioner
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DatabaseProvisioner> _logger;

        public DatabaseProvisioner(IConfiguration config, ILogger<DatabaseProvisioner> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> CreateUserDatabaseAsync(string username)
        {
            // Tạo tên DB an toàn
            var safeName = new string(username
                .Where(c => char.IsLetterOrDigit(c) || c == '_')
                .ToArray());

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "user";

            var guid = Guid.NewGuid().ToString("N");
            var dbName = $"ImageDB_{safeName}_{guid}";
            if (dbName.Length > 60) dbName = dbName.Substring(0, 60);

            var masterConn = _config.GetConnectionString("MasterConnection")
                ?? "Data Source=.;Database=master;Integrated Security=True;TrustServerCertificate=True;";

            await using var connection = new SqlConnection(masterConn);
            await connection.OpenAsync();

            // 1. Tạo database
            var createDbSql = $@"
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}')
BEGIN
    CREATE DATABASE [{dbName}];
END";

            await using (var cmd = new SqlCommand(createDbSql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Created database {DbName} for user {Username}", dbName, username);

            // 2. Tạo bảng Images
            var userDbConnStr = masterConn.Replace("Database=master", $"Database={dbName}");

            await using var userConn = new SqlConnection(userDbConnStr);
            await userConn.OpenAsync();

            var createTableSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Images')
BEGIN
    CREATE TABLE [Images] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [FileName] NVARCHAR(255) NOT NULL,
        [FileDescription] NVARCHAR(MAX) NULL,
        [FileExtension] NVARCHAR(20) NOT NULL,
        [FileSizeInBytes] BIGINT NOT NULL,
        [FilePath] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UploadedBy] NVARCHAR(256) NOT NULL DEFAULT ''
    );
END
ELSE
BEGIN
    -- Nếu bảng đã tồn tại thì thêm cột (cho database cũ)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Images') AND name = 'UploadedBy')
    BEGIN
        ALTER TABLE [Images] ADD [UploadedBy] NVARCHAR(256) NOT NULL DEFAULT '';
    END
END";

            await using (var cmd = new SqlCommand(createTableSql, userConn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            return dbName;
        }
    }
}