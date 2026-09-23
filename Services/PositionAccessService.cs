using System.Globalization;
using CvManagementSystem.Data;
using CvManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Services;

public class PositionAccessService
{
    private readonly ApplicationDbContext _context;

    public PositionAccessService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Position>> GetVisiblePositionsAsync(
        string? userId,
        bool unrestricted)
    {
        var query = _context.Positions
            .AsNoTracking()
            .Include(x => x.AccessRules)
                .ThenInclude(x => x.AttributeDefinition)
            .OrderByDescending(x => x.UpdatedAt);

        var positions =
            await query.ToListAsync();

        // Recruiters and Administrators can see the
        // complete shared position list.
        if (unrestricted)
        {
            return positions;
        }

        // Anonymous users can only browse public positions.
        if (string.IsNullOrWhiteSpace(userId))
        {
            return positions
                .Where(x => x.IsPublic)
                .ToList();
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .Include(x => x.AttributeValues)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

        if (profile == null)
        {
            return positions
                .Where(x => x.IsPublic)
                .ToList();
        }

        return positions
            .Where(
                position =>
                    position.IsPublic ||
                    IsAuthorized(
                        position,
                        profile))
            .ToList();
    }

    public async Task<bool> CanAccessAsync(
        int positionId,
        string userId)
    {
        var position =
            await _context.Positions
                .AsNoTracking()
                .Include(x => x.AccessRules)
                    .ThenInclude(x => x.AttributeDefinition)
                .FirstOrDefaultAsync(
                    x => x.Id == positionId);

        if (position == null)
        {
            return false;
        }

        // A public position never requires an access-rule evaluation, so it
        // must not require a CandidateProfile row either. A brand-new
        // candidate has no profile until they first save one (see
        // ProfileController.Index), and should still be able to access
        // public positions in the meantime.
        if (position.IsPublic)
        {
            return true;
        }

        var profile =
            await _context.CandidateProfiles
                .AsNoTracking()
                .Include(x => x.AttributeValues)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

        if (profile == null)
        {
            return false;
        }

        return IsAuthorized(
            position,
            profile);
    }

    public static bool IsAuthorized(
        Position position,
        CandidateProfile profile)
    {
        if (position.IsPublic)
        {
            return true;
        }

        // A restricted position without rules has
        // no candidate authorization condition.
        if (position.AccessRules.Count == 0)
        {
            return false;
        }

        var values =
            profile.AttributeValues
                .GroupBy(x => x.AttributeDefinitionId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Last().Value);

        return position.AccessRules.All(
            rule =>
                EvaluateRule(
                    rule,
                    values));
    }

    private static bool EvaluateRule(
        PositionAccessRule rule,
        Dictionary<int, string?> values)
    {
        if (!values.TryGetValue(
                rule.AttributeDefinitionId,
                out var candidateValue))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(candidateValue))
        {
            return false;
        }

        var requiredValue =
            rule.Value.Trim();

        var actualValue =
            candidateValue.Trim();

        return rule.AttributeDefinition.DataType switch
        {
            AttributeDataType.String =>
                EvaluateString(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            AttributeDataType.Text =>
                EvaluateString(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            AttributeDataType.Dropdown =>
                EvaluateString(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            AttributeDataType.Image =>
                EvaluateString(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            AttributeDataType.Numeric =>
                EvaluateNumeric(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            AttributeDataType.Date =>
                EvaluateDate(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            AttributeDataType.Boolean =>
                EvaluateBoolean(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            AttributeDataType.Period =>
                EvaluatePeriod(
                    actualValue,
                    requiredValue,
                    rule.Operator),

            _ => false
        };
    }

    private static bool EvaluateString(
        string actual,
        string expected,
        AccessRuleOperator op)
    {
        var comparison =
            StringComparison.OrdinalIgnoreCase;

        return op switch
        {
            AccessRuleOperator.Equals =>
                string.Equals(
                    actual,
                    expected,
                    comparison),

            AccessRuleOperator.NotEquals =>
                !string.Equals(
                    actual,
                    expected,
                    comparison),

            AccessRuleOperator.Contains =>
                actual.Contains(
                    expected,
                    comparison),

            AccessRuleOperator.StartsWith =>
                actual.StartsWith(
                    expected,
                    comparison),

            _ => false
        };
    }

    private static bool EvaluateNumeric(
        string actual,
        string expected,
        AccessRuleOperator op)
    {
        if (!decimal.TryParse(
                actual,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var actualNumber))
        {
            return false;
        }

        if (!decimal.TryParse(
                expected,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var expectedNumber))
        {
            return false;
        }

        return op switch
        {
            AccessRuleOperator.Equals =>
                actualNumber == expectedNumber,

            AccessRuleOperator.NotEquals =>
                actualNumber != expectedNumber,

            AccessRuleOperator.GreaterThan =>
                actualNumber > expectedNumber,

            AccessRuleOperator.GreaterThanOrEqual =>
                actualNumber >= expectedNumber,

            AccessRuleOperator.LessThan =>
                actualNumber < expectedNumber,

            AccessRuleOperator.LessThanOrEqual =>
                actualNumber <= expectedNumber,

            _ => false
        };
    }

    private static bool EvaluateDate(
        string actual,
        string expected,
        AccessRuleOperator op)
    {
        if (!DateOnly.TryParse(
                actual,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var actualDate))
        {
            return false;
        }

        if (!DateOnly.TryParse(
                expected,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var expectedDate))
        {
            return false;
        }

        return op switch
        {
            AccessRuleOperator.Equals =>
                actualDate == expectedDate,

            AccessRuleOperator.NotEquals =>
                actualDate != expectedDate,

            AccessRuleOperator.GreaterThan =>
                actualDate > expectedDate,

            AccessRuleOperator.GreaterThanOrEqual =>
                actualDate >= expectedDate,

            AccessRuleOperator.LessThan =>
                actualDate < expectedDate,

            AccessRuleOperator.LessThanOrEqual =>
                actualDate <= expectedDate,

            _ => false
        };
    }

    private static bool EvaluateBoolean(
        string actual,
        string expected,
        AccessRuleOperator op)
    {
        if (!bool.TryParse(
                actual,
                out var actualBoolean))
        {
            return false;
        }

        if (!bool.TryParse(
                expected,
                out var expectedBoolean))
        {
            return false;
        }

        return op switch
        {
            AccessRuleOperator.Equals =>
                actualBoolean == expectedBoolean,

            AccessRuleOperator.NotEquals =>
                actualBoolean != expectedBoolean,

            _ => false
        };
    }

    private static bool EvaluatePeriod(
        string actual,
        string expected,
        AccessRuleOperator op)
    {
        return op switch
        {
            AccessRuleOperator.Equals =>
                string.Equals(
                    actual,
                    expected,
                    StringComparison.OrdinalIgnoreCase),

            AccessRuleOperator.NotEquals =>
                !string.Equals(
                    actual,
                    expected,
                    StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }
}