using ExamSimulator.Web.Domain.ExamProfiles;
using ExamSimulator.Web.Domain.Identity;
using ExamSimulator.Web.Domain.Questions;
using Microsoft.AspNetCore.Identity;

namespace ExamSimulator.Web.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ExamSimulatorDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        await SeedIdentityAsync(userManager, roleManager, configuration);
        SeedQuestions(db);
    }

    private static async Task SeedIdentityAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        const string adminRole = "Admin";
        const string adminEmail = "admin@examsimulator.local";

        if (!await roleManager.RoleExistsAsync(adminRole))
            await roleManager.CreateAsync(new IdentityRole(adminRole));

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var password = configuration["Seeding:AdminPassword"] ?? "Dev@dmin1!";
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, password);
            await userManager.AddToRoleAsync(admin, adminRole);
        }
    }

    private static void SeedQuestions(ExamSimulatorDbContext db)
    {
        if (!db.ExamProfiles.Any())
        {
            db.ExamProfiles.Add(new ExamProfile("az-204", "Azure Developer Associate AZ-204",
                "Covers Azure compute, storage, security, and monitoring for the AZ-204 certification exam."));
            db.SaveChanges();
        }

        if (db.Questions.Any())
            return;

        db.Questions.AddRange(
            new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Easy,
                "Which Azure service provides a **fully managed** platform for running containerized applications without managing the underlying infrastructure?",
                ["Azure Virtual Machines", "Azure Container Apps", "Azure Kubernetes Service", "Azure App Service"],
                [1], "compute",
                "`Azure Container Apps` is a fully managed serverless container service built on top of Kubernetes. Unlike `Azure Kubernetes Service`, it abstracts away cluster management entirely."),

            new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Medium,
                "What is the default number of days before a Blob Storage lifecycle management policy transitions a blob between access tiers (`hot` → `cool` → `archive`)?\n\n> Hint: think about what happens when no days value is configured.",
                ["1 day", "7 days", "30 days", "No default — the number of days must be set explicitly"],
                [3], "storage",
                "Lifecycle management policies **require** an explicit `daysAfterModificationGreaterThan` or equivalent condition. There is no built-in default."),

            new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Hard,
                "An Azure Function uses a **Service Bus trigger**. The function throws an unhandled exception after receiving a message. What happens to the message by default?",
                ["It is immediately deleted from the queue", "It is moved straight to the dead-letter queue", "It is abandoned and retried up to the `maxDeliveryCount`, then dead-lettered", "It is re-queued indefinitely"],
                [2], "messaging",
                "When the function fails, the Service Bus SDK **abandons** the message lock. The broker retries delivery until `maxDeliveryCount` is reached, after which the message is moved to the `$DeadLetterQueue`."),

            new Question(Guid.NewGuid(), "az-204", QuestionType.MultipleChoice, Difficulty.Medium,
                "Which of the following are valid **Azure App Service deployment slot** names? (Select all that apply)\n\n*App Service supports up to 20 named slots per app.*",
                ["production", "staging", "testing", "development", "preview"],
                [0, 1, 2, 3, 4], "app-service",
                "Any name is valid — `production`, `staging`, `testing`, `development`, and `preview` are all legitimate slot names. The slot named `production` is the live slot by default."),

            new Question(Guid.NewGuid(), "az-204", QuestionType.MultipleChoice, Difficulty.Hard,
                "A **web API** needs to call a downstream API on behalf of the signed-in user. Which MSAL authentication flows support this scenario? (Select all that apply)",
                ["Authorization code flow", "On-Behalf-Of (`OBO`) flow", "Client credentials flow", "Device code flow"],
                [1, 2], "authentication",
                "The **On-Behalf-Of (OBO)** flow lets a mid-tier API exchange an incoming access token for a new token scoped to the downstream API.\n\n**Client credentials** can also be used when the API calls the downstream API as itself (not on behalf of a user), e.g. for background processing."),

            new Question(Guid.NewGuid(), "az-204", QuestionType.Ordering, Difficulty.Medium,
                "Place the following steps in the **correct order** to grant an Azure App Service access to Key Vault secrets using a **managed identity**:\n\n```\nNo passwords or connection strings stored in code.\n```",
                [
                    "Enable the **system-assigned managed identity** on the App Service",
                    "Grant the identity the `Key Vault Secrets User` role on the Key Vault",
                    "Add an app setting using the Key Vault reference syntax: `@Microsoft.KeyVault(SecretUri=<uri>)`",
                    "Restart the App Service to apply the updated application settings"
                ],
                [0, 1, 2, 3], "keyvault",
                "The identity must exist before you can assign a role to it. The role must be in place before the Key Vault reference resolves. The app setting takes effect only after the service restarts and re-reads configuration."),

            new Question(Guid.NewGuid(), "az-204", QuestionType.BuildList, Difficulty.Medium,
                "From the list below, identify the valid **Azure Blob Storage access tiers** and arrange them in order of **increasing storage cost per GB** (cheapest storage first).\n\n*Not all items in the list are valid access tiers.*",
                ["Hot", "Cool", "Cold", "Archive", "Warm", "Premium"],
                [3, 2, 1, 0], "storage",
                "Azure Blob Storage has four access tiers: **Archive** (cheapest to store, most expensive to read), **Cold**, **Cool**, and **Hot** (most expensive to store, cheapest to read).\n\n`Warm` and `Premium` are not standard Blob Storage access tiers."),

            new Question(Guid.NewGuid(), "az-204", QuestionType.Matching, Difficulty.Medium,
                "Match each **Azure messaging service** with its primary delivery guarantee.",
                ["Azure Service Bus", "Azure Event Hubs", "Azure Event Grid", "Azure Queue Storage"],
                [0, 1, 2, 3], "messaging",
                "**Service Bus** → at-least-once (with duplicate detection for exactly-once). **Event Hubs** → at-least-once, ordered within a partition. **Event Grid** → at-least-once with retry policy. **Queue Storage** → at-least-once, no ordering guarantees.",
                ["At-least-once, ordered within a partition (streaming)", "At-least-once with configurable retry (event-driven)", "At-least-once with duplicate detection option (messaging)", "At-least-once, no ordering guarantees (simple queuing)"])
        );

        db.SaveChanges();
    }

    // Keep the synchronous overload for callers that seed questions only (tests, etc.)
    public static void Seed(ExamSimulatorDbContext db) => SeedQuestions(db);
}
