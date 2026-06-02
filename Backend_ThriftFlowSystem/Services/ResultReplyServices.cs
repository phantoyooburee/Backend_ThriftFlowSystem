using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;

namespace ThriftFlowSystem.Services;

public class ResultReplyServices : IResultReplyServices
{
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "error_status:all";

    public ResultReplyServices(IConfiguration config, IMemoryCache cache)
    {
        _config = config;
        _cache = cache;
    }

    public int MapReply(ResultListReply reply)
    {
        int status;
        var code = reply.Result.Code;
        switch (code)
        {
            case "201": status = StatusCodes.Status201Created; break;
            case "200": status = StatusCodes.Status200OK; break;
            case "409": status = StatusCodes.Status409Conflict; break;
            case "400": status = StatusCodes.Status400BadRequest; break;
            case "401": status = StatusCodes.Status401Unauthorized; break;
            case "403": status = StatusCodes.Status403Forbidden; break;
            default: status = StatusCodes.Status500InternalServerError; break;
        }
        return status;
    }

    public async Task<ErrorStatus?> ErrorMessage(int errorCode)
    {
        var connStr = _config.GetConnectionString("DBContext");

        const string sql = "SELECT ErrorCode, ErrorDesc FROM ErrorMessages";

        var allErrors = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            // use NpgsqlConnection( PostgreSQL ADO.NET provider) to connect to the database and query the error messages)
            using var connection = new NpgsqlConnection(connStr);
            await connection.OpenAsync();


            var rows = await connection.QueryAsync<ErrorStatus>(sql);

            return rows.ToDictionary(r => r.ErrorCode, r => r);
        });

        return allErrors!.TryGetValue(errorCode, out var status) ? status : null;
    }
}

