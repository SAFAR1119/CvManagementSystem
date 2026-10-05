using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers;

public class OdooIntegrationController : Controller
{
    private readonly ApplicationDbContext _context;

    public OdooIntegrationController(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // API TOKEN PAGE
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpGet]
    public async Task<IActionResult> ApiToken(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        var token = await _context.PositionApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PositionId == id);

        var model = new OdooApiTokenViewModel
        {
            PositionId = position.Id,
            PositionTitle = position.Title,
            CreatedAt = token?.CreatedAt,
            LastUsedAt = token?.LastUsedAt,
            GeneratedToken = TempData["GeneratedApiToken"] as string
        };

        return View(model);
    }

    // =========================================================
    // GENERATE / REGENERATE API TOKEN
    // =========================================================

    [Authorize(Roles = "Recruiter,Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateApiToken(int id)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (position == null)
        {
            return NotFound();
        }

        var existingToken = await _context.PositionApiTokens
            .FirstOrDefaultAsync(
                x => x.PositionId == id);

        if (existingToken != null)
        {
            _context.PositionApiTokens.Remove(existingToken);
        }

        var plainToken = GenerateSecureToken();

        var tokenHash = ComputeTokenHash(plainToken);

        var apiToken = new PositionApiToken
        {
            PositionId = position.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.PositionApiTokens.Add(apiToken);

        await _context.SaveChangesAsync();

        TempData["GeneratedApiToken"] = plainToken;

        return RedirectToAction(
            nameof(ApiToken),
            new
            {
                id = position.Id
            });
    }

    // =========================================================
    // EXTERNAL ODOO API
    //
    // Existing endpoint:
    // GET /api/integration/positions/{positionId}/aggregate
    //
    // Requires:
    // X-Position-Api-Token
    // =========================================================

    [AllowAnonymous]
    [HttpGet(
        "api/integration/positions/{positionId:int}/aggregate")]
    public async Task<IActionResult> GetPositionAggregate(
        int positionId)
    {
        var providedToken =
            Request.Headers["X-Position-Api-Token"]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedToken))
        {
            return Unauthorized(new
            {
                error =
                    "Missing X-Position-Api-Token header."
            });
        }

        var tokenHash =
            ComputeTokenHash(providedToken);

        var apiToken = await _context.PositionApiTokens
            .FirstOrDefaultAsync(
                x =>
                    x.PositionId == positionId &&
                    x.TokenHash == tokenHash);

        if (apiToken == null)
        {
            return Unauthorized(new
            {
                error =
                    "Invalid API token for this position."
            });
        }

        return await GetPositionAggregateInternal(
            positionId,
            apiToken);
    }

    // =========================================================
    // EXTERNAL ODOO API - TOKEN ONLY
    //
    // Odoo can call:
    //
    // GET /api/integration/position
    //
    // using only:
    //
    // X-Position-Api-Token
    //
    // The token itself identifies the position.
    // =========================================================

    [AllowAnonymous]
    [HttpGet("api/integration/position")]
    public async Task<IActionResult>
        GetPositionAggregateByToken()
    {
        var providedToken =
            Request.Headers["X-Position-Api-Token"]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedToken))
        {
            return Unauthorized(new
            {
                error =
                    "Missing X-Position-Api-Token header."
            });
        }

        var tokenHash =
            ComputeTokenHash(providedToken);

        var apiToken = await _context.PositionApiTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash);

        if (apiToken == null)
        {
            return Unauthorized(new
            {
                error = "Invalid API token."
            });
        }

        return await GetPositionAggregateInternal(
            apiToken.PositionId,
            apiToken);
    }

    // =========================================================
    // COMMON AGGREGATION LOGIC
    // =========================================================

    private async Task<IActionResult>
        GetPositionAggregateInternal(
            int positionId,
            PositionApiToken apiToken)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(x => x.Attributes)
                .ThenInclude(x =>
                    x.AttributeDefinition)
                    .ThenInclude(x =>
                        x.Category)
            .FirstOrDefaultAsync(
                x => x.Id == positionId);

        if (position == null)
        {
            return NotFound(new
            {
                error = "Position not found."
            });
        }

        var publishedCvs =
            await _context.Cvs
                .AsNoTracking()
                .Where(
                    x =>
                        x.PositionId == positionId &&
                        x.IsPublished)
                .Include(x => x.AttributeValues)
                .ToListAsync();

        apiToken.LastUsedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result =
            new OdooPositionAggregateViewModel
            {
                ApiVersion = 1,

                PositionId =
                    position.Id,

                PositionTitle =
                    position.Title,

                Department =
                    position.Department,

                Experience =
                    position.Experience.ToString(),

                EmploymentType =
                    position.EmploymentType.ToString(),

                WorkMode =
                    position.WorkMode.ToString(),

                Description =
                    position.Description,

                IsPublic =
                    position.IsPublic,

                MaxProjects =
                    position.MaxProjects,

                PublishedCvCount =
                    publishedCvs.Count
            };

        foreach (
            var positionAttribute
            in position.Attributes
                .OrderBy(x => x.SortOrder))
        {
            var definition =
                positionAttribute
                    .AttributeDefinition;

            var values =
                publishedCvs
                    .SelectMany(
                        x =>
                            x.AttributeValues
                                .Where(
                                    v =>
                                        v.AttributeDefinitionId ==
                                        definition.Id))
                    .Select(x => x.Value)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .Select(
                        x =>
                            x!.Trim())
                    .ToList();

            result.Attributes.Add(
                BuildAttributeAggregate(
                    definition,
                    positionAttribute,
                    values));
        }

        return Ok(result);
    }

    // =========================================================
    // ATTRIBUTE AGGREGATION
    // =========================================================

    private static OdooAttributeAggregateViewModel
        BuildAttributeAggregate(
            AttributeDefinition definition,
            PositionAttribute positionAttribute,
            List<string> values)
    {
        var dataTypeName =
            definition.DataType.ToString();

        var aggregate =
            new OdooAggregateValueViewModel
            {
                ValueCount =
                    values.Count
            };

        // =====================================================
        // NUMERIC
        // =====================================================

        if (dataTypeName.Contains(
                "Numeric",
                StringComparison.OrdinalIgnoreCase))
        {
            var numbers =
                values
                    .Select(ParseDecimal)
                    .Where(
                        x => x.HasValue)
                    .Select(
                        x => x!.Value)
                    .ToList();

            aggregate.Kind =
                "numeric";

            if (numbers.Count > 0)
            {
                aggregate.Average =
                    Math.Round(
                        numbers.Average(),
                        2);

                aggregate.Minimum =
                    numbers.Min();

                aggregate.Maximum =
                    numbers.Max();
            }
        }

        // =====================================================
        // BOOLEAN
        // =====================================================

        else if (
            dataTypeName.Contains(
                "Boolean",
                StringComparison.OrdinalIgnoreCase))
        {
            var booleanValues =
                values
                    .Select(ParseBoolean)
                    .Where(
                        x => x.HasValue)
                    .Select(
                        x => x!.Value)
                    .ToList();

            aggregate.Kind =
                "boolean";

            aggregate.TrueCount =
                booleanValues.Count(
                    x => x);

            aggregate.FalseCount =
                booleanValues.Count(
                    x => !x);
        }

        // =====================================================
        // DATE
        // =====================================================

        else if (
            dataTypeName.Equals(
                "Date",
                StringComparison.OrdinalIgnoreCase))
        {
            aggregate.Kind =
                "date";

            aggregate.PopularValues =
                values
                    .GroupBy(
                        x => x,
                        StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(
                        x => x.Count())
                    .ThenBy(
                        x => x.Key)
                    .Take(5)
                    .Select(
                        x =>
                            new OdooPopularValueViewModel
                            {
                                Value = x.Key,
                                Count = x.Count()
                            })
                    .ToList();
        }

        // =====================================================
        // PERIOD
        // =====================================================

        else if (
            dataTypeName.Contains(
                "Period",
                StringComparison.OrdinalIgnoreCase))
        {
            aggregate.Kind =
                "period";

            aggregate.PopularValues =
                values
                    .GroupBy(
                        x => x,
                        StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(
                        x => x.Count())
                    .ThenBy(
                        x => x.Key)
                    .Take(5)
                    .Select(
                        x =>
                            new OdooPopularValueViewModel
                            {
                                Value = x.Key,
                                Count = x.Count()
                            })
                    .ToList();
        }

        // =====================================================
        // TEXT / STRING / DROPDOWN / OTHER
        // =====================================================

        else
        {
            aggregate.Kind =
                "categorical";

            aggregate.PopularValues =
                values
                    .GroupBy(
                        x => x,
                        StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(
                        x => x.Count())
                    .ThenBy(
                        x => x.Key)
                    .Take(5)
                    .Select(
                        x =>
                            new OdooPopularValueViewModel
                            {
                                Value = x.Key,
                                Count = x.Count()
                            })
                    .ToList();
        }

        return new OdooAttributeAggregateViewModel
        {
            AttributeDefinitionId =
                definition.Id,

            Title =
                definition.Name,

            Type =
                dataTypeName,

            IsRequired =
                positionAttribute.IsRequired,

            SortOrder =
                positionAttribute.SortOrder,

            Aggregate =
                aggregate
        };
    }

    // =========================================================
    // SECURE TOKEN GENERATION
    // =========================================================

    private static string GenerateSecureToken()
    {
        var bytes =
            RandomNumberGenerator.GetBytes(48);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }

    // =========================================================
    // TOKEN HASH
    // =========================================================

    private static string ComputeTokenHash(
        string token)
    {
        var bytes =
            Encoding.UTF8.GetBytes(token);

        var hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    // =========================================================
    // DECIMAL PARSER
    // =========================================================

    private static decimal? ParseDecimal(
        string value)
    {
        if (
            decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var result))
        {
            return result;
        }

        return null;
    }

    // =========================================================
    // BOOLEAN PARSER
    // =========================================================

    private static bool? ParseBoolean(
        string value)
    {
        if (
            bool.TryParse(
                value,
                out var result))
        {
            return result;
        }

        if (value == "1")
        {
            return true;
        }

        if (value == "0")
        {
            return false;
        }

        return null;
    }
}