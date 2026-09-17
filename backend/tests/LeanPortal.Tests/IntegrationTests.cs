using System.Net;
using System.Text.Json.Nodes;
using LeanPortal.Api.Controllers.Public;
using LeanPortal.Domain.Entities;
using LeanPortal.Infrastructure.Services.Integrations;

namespace LeanPortal.Tests;

/// <summary>
/// The template filling behind Zoho's custom fields and every integration's API
/// call. What a visitor types goes through this into a request sent to someone
/// else's system, so it must not be able to change the request's shape.
/// </summary>
public class JsonTemplateTests
{
    [Fact]
    public void A_quote_in_what_was_typed_stays_inside_the_value()
    {
        var rendered = JsonTemplate.Render("""{ "cf": { "cf_organisation_name": "{{organisation}}" } }""",
            new Dictionary<string, string?> { ["organisation"] = "Acme\", \"priority\": \"High" });

        var cf = Assert.IsType<JsonObject>(rendered["cf"]);
        Assert.Single(cf);
        Assert.Equal("Acme\", \"priority\": \"High", cf["cf_organisation_name"]!.GetValue<string>());
        Assert.False(rendered.ContainsKey("priority"));
    }

    [Fact]
    public void A_field_that_is_only_an_empty_placeholder_becomes_null()
    {
        // Zoho reads "" as a value and null as "not given"; an optional custom field
        // left blank on the form must arrive as the second.
        var rendered = JsonTemplate.Render("""{ "cf_udyam_number": "{{udyamNumber}}", "note": "Udyam: {{udyamNumber}}" }""",
            new Dictionary<string, string?> { ["udyamNumber"] = null });

        Assert.Null(rendered["cf_udyam_number"]);
        Assert.Equal("Udyam: ", rendered["note"]!.GetValue<string>());
    }

    [Fact]
    public void An_address_escapes_what_is_put_into_it()
    {
        var url = JsonTemplate.RenderUrl("https://api.example.in/verify?cert={{certificateNumber}}",
            new Dictionary<string, string?> { ["certificateNumber"] = "LEAN/2026&admin=1" });

        Assert.Equal("https://api.example.in/verify?cert=LEAN%2F2026%26admin%3D1", url);
    }

    [Fact]
    public void A_placeholder_nothing_fills_is_refused_on_save()
    {
        var problem = JsonTemplate.Validate("""{ "cf": { "x": "{{userTpye}}" } }""", ["userType", "email"]);

        Assert.NotNull(problem);
        Assert.Contains("{{userTpye}}", problem);
    }

    [Theory]
    [InlineData("data.units[1].name", "Beta Forge")]
    [InlineData("Data.Units[0].Name", "Alpha Tools")]
    [InlineData("$.data.total", "2")]
    [InlineData("data.units[5].name", null)]
    [InlineData("data.missing.name", null)]
    public void Values_are_read_by_dotted_path(string path, string? expected)
    {
        var json = JsonNode.Parse("""
            { "data": { "total": 2, "units": [ { "name": "Alpha Tools" }, { "name": "Beta Forge" } ] } }
            """);

        Assert.Equal(expected, JsonTemplate.Display(JsonTemplate.Select(json, path)));
    }
}

/// <summary>
/// The server calls addresses typed into the console. These are the places a
/// hijacked account would point it to steal what the network can reach.
/// </summary>
public class OutboundAddressTests
{
    [Theory]
    [InlineData("169.254.169.254")] // Azure's instance metadata service
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.20.0.5")]
    [InlineData("192.168.1.10")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("fd00::1")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:10.0.0.1")] // a private address dressed as IPv6
    public void Private_and_local_addresses_are_refused(string address) =>
        Assert.False(OutboundHttp.IsPublic(IPAddress.Parse(address)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("20.219.21.200")]
    [InlineData("2606:4700:4700::1111")]
    public void Public_addresses_are_allowed(string address) =>
        Assert.True(OutboundHttp.IsPublic(IPAddress.Parse(address)));
}

public class GrievanceMatrixTests
{
    private const string Matrix = """
        {
          "labels": ["User type", "Complaint or query", "Related to", "Specific issue"],
          "options": [
            { "name": "MSME", "children": [
              { "name": "Complaint", "children": [
                { "name": "Assessor", "children": [ { "name": "Ethical Issue" } ] }
              ] },
              { "name": "Query", "children": [
                { "name": "LEAN", "children": [ { "name": "Payment Related" } ] }
              ] }
            ] }
          ]
        }
        """;

    private static readonly LeanPortal.Application.Contracts.GrievanceMatrixDto Parsed =
        GrievanceMatrix.Parse("QCI", Matrix)!;

    [Fact]
    public void A_complete_path_is_accepted() =>
        Assert.True(GrievanceMatrix.IsValidPath(Parsed, "MSME", "Query", "LEAN", "Payment Related"));

    [Fact]
    public void A_choice_sent_past_the_end_of_a_branch_is_refused()
    {
        var shallow = GrievanceMatrix.Parse("QCI", """
            { "options": [ { "name": "MSME", "children": [ { "name": "Complaint" } ] } ] }
            """)!;

        Assert.True(GrievanceMatrix.IsValidPath(shallow, "MSME", "Complaint", null, null));

        // Nothing follows "Complaint" in this matrix, so nothing may be sent for the
        // levels after it: those words go straight into Zoho's picklist fields.
        Assert.False(GrievanceMatrix.IsValidPath(shallow, "MSME", "Complaint", null, "anything at all"));
        Assert.False(GrievanceMatrix.IsValidPath(shallow, "MSME", "Complaint", "made up", null));
    }

    [Theory]
    [InlineData("MSME", "Query", "LEAN", null)]              // stops short of the last level
    [InlineData("MSME", "Query", "Assessor", "Ethical Issue")] // mixes two branches
    [InlineData("MSME", "Complaint", "Assessor", "Bribery")]  // not an option Zoho has
    [InlineData(null, null, null, null)]                      // nothing chosen at all
    public void Anything_else_is_refused(string? a, string? b, string? c, string? d) =>
        Assert.False(GrievanceMatrix.IsValidPath(Parsed, a, b, c, d));

    [Fact]
    public void An_option_listed_twice_at_one_level_is_refused_on_save()
    {
        var problem = GrievanceMatrix.Validate("""
            { "options": [ { "name": "MSME" }, { "name": "msme" } ] }
            """);

        Assert.NotNull(problem);
        Assert.Contains("twice", problem);
    }

    [Fact]
    public void The_seeded_matrix_is_sound() =>
        Assert.Null(GrievanceMatrix.Validate(System.Text.Json.JsonSerializer.Serialize(
            LeanPortal.Infrastructure.Persistence.Seed.DataSeeder.DefaultGrievanceMatrix)));
}

public class HelpdeskTicketTests
{
    private static readonly ContactMessage Enquiry = new()
    {
        Id = 42,
        Name = "Asha Devi Sharma",
        Email = "asha@example.com",
        Phone = "9876543210",
        Subject = "Payment not reflected",
        Message = "Paid on 3 Sept.\n<b>Please</b> check.",
        UserType = "MSME",
        IssueType = "Query",
        IssueCategory = "LEAN",
        IssueSubCategory = "Payment Related",
    };

    [Fact]
    public void The_seeded_template_fills_the_ndie_custom_fields_from_the_matrix()
    {
        var ticket = HelpdeskDispatcher.BuildTicket(
            new HelpdeskConnection { DepartmentId = "185", TicketTemplate = HelpdeskDispatcher.DefaultTicketTemplate },
            Enquiry, "C1", null);

        var cf = Assert.IsType<JsonObject>(ticket["cf"]);
        Assert.Equal("MSME", cf["cf_user_role"]!.GetValue<string>());
        Assert.Equal("Query", cf["cf_issue_type"]!.GetValue<string>());
        Assert.Equal("LEAN", cf["cf_issue_category"]!.GetValue<string>());
        Assert.Equal("Payment Related", cf["cf_issue_sub_category"]!.GetValue<string>());
        Assert.Equal("Web", ticket["channel"]!.GetValue<string>());
        Assert.Equal("185", ticket["departmentId"]!.GetValue<string>());
    }

    [Fact]
    public void The_template_cannot_change_whose_ticket_it_is_or_its_files()
    {
        var ticket = HelpdeskDispatcher.BuildTicket(
            new HelpdeskConnection
            {
                DepartmentId = "185",
                TicketTemplate = """{ "contactId": "someone-else", "uploads": ["x"], "channel": "Platform" }""",
            },
            Enquiry, "C1", new JsonArray("U1", "U2"));

        Assert.Equal("C1", ticket["contactId"]!.GetValue<string>());
        Assert.Equal(["U1", "U2"], ticket["uploads"]!.AsArray().Select(n => n!.GetValue<string>()));
        // Anything else it may change - Zoho's file note uses "Platform".
        Assert.Equal("Platform", ticket["channel"]!.GetValue<string>());
    }

    [Fact]
    public void The_message_is_escaped_in_the_description()
    {
        var ticket = HelpdeskDispatcher.BuildTicket(new HelpdeskConnection { DepartmentId = "1" }, Enquiry, "C1", null);
        var description = ticket["description"]!.GetValue<string>();

        Assert.Contains("&lt;b&gt;Please&lt;/b&gt;", description);
        Assert.Contains("LEAN-ENQ-000042", description);
    }

    [Theory]
    [InlineData("Asha Devi Sharma", "Asha Devi", "Sharma")]
    [InlineData("Ramesh", null, "Ramesh")]
    public void A_name_is_split_the_way_zoho_needs_it(string name, string? first, string last)
    {
        var values = HelpdeskDispatcher.ValuesFor(new ContactMessage { Name = name });

        Assert.Equal(first, values["firstName"]);
        Assert.Equal(last, values["lastName"]);
    }
}

public class AssistantMatchingTests
{
    [Theory]
    [InlineData("registration", "regist")]
    [InlineData("certificate", "certif")]
    [InlineData("eligibility", "eligib")]
    [InlineData("levels", "level")]
    [InlineData("subsidies", "subsid")]
    [InlineData("fees", "fee")]
    public void Words_are_reduced_to_a_stem_their_relatives_share(string word, string stem) =>
        Assert.Equal(stem, AssistantController.Stem(word));

    [Fact]
    public void The_word_itself_outscores_a_relative_of_it()
    {
        var terms = AssistantController.Terms("registration");

        var exact = AssistantController.Score(terms, "Registration opens in October.", 1);
        var related = AssistantController.Score(terms, "How to register your enterprise.", 1);
        var unrelated = AssistantController.Score(terms, "Scheme levels and fees.", 1);

        Assert.True(exact > related);
        Assert.True(related > unrelated);
        Assert.Equal(0, unrelated);
    }
}
