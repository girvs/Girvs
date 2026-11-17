using System.Linq.Expressions;
using Girvs.BusinessBasis.Entities;
using Girvs.BusinessBasis.Repositories;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Extensions;
using Girvs.Extensions.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var compilations = 0;
var detections = 0;
var options = new DbContextOptionsBuilder<ProbeContext>()
    .UseSqlite("Data Source=:memory:")
    .LogTo(_ => compilations++, [CoreEventId.QueryCompilationStarting])
    .Options;
using var context = new ProbeContext(options);
context.Database.OpenConnection();
context.Database.EnsureCreated();
context.Rows.AddRange(
    new ProbeRow { Id = 1, Name = "a", Count = 30 },
    new ProbeRow { Id = 2, Name = "b", Count = 10 },
    new ProbeRow { Id = 3, Name = "c", Count = 20 });
context.SaveChanges();
context.ChangeTracker.Clear();

// 记录真实执行触发的编译次数，独立于机器速度。
var before = compilations;
foreach (var name in new[] { "a", "b", "c" })
    _ = context.Rows.WhereEqual(nameof(ProbeRow.Name), name).ToList();
Console.WriteLine($"WhereEqual/3 values: query compilations={compilations - before}");
Console.WriteLine(context.Rows.WhereEqual(nameof(ProbeRow.Name), "a").ToQueryString());

before = compilations;
foreach (var name in new[] { "a", "b", "c" })
    _ = context.Rows.Where(row => row.Name == name).ToList();
Console.WriteLine($"Captured parameter/3 values: query compilations={compilations - before}");

var condition = new ProbeTenantCondition();
before = compilations;
foreach (var tenant in Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()))
    _ = context.Rows.Where(condition.ForTenant(tenant)).ToList();
Console.WriteLine($"Girvs tenant condition/3 tenants: query compilations={compilations - before}");

var fullQuery = context.Rows.OrderByDescending(row => row.Count).Select(row => row.Name);
var projectionThenSort = context.Rows.SelectProperties([nameof(ProbeRow.Name)])
    .OrderByDescending(nameof(ProbeRow.Count));
Console.WriteLine("Projection then sorting by omitted Count:");
try
{
    Console.WriteLine(projectionThenSort.ToQueryString());
    Console.WriteLine("Returned order: " + string.Join(",", projectionThenSort.Select(row => row.Name).ToList()));
}
catch (Exception exception)
{
    Console.WriteLine(exception.GetType().Name + ": " + exception.Message);
}
Console.WriteLine("Expected order: " + string.Join(",", fullQuery.ToList()));

context.ChangeTracker.Clear();
_ = context.Rows.ToList();
Console.WriteLine($"Full entity tracked entries: {context.ChangeTracker.Entries().Count()}");
context.ChangeTracker.Clear();
_ = context.Rows.SelectProperties([nameof(ProbeRow.Name)]).ToList();
Console.WriteLine($"SelectProperties(Name) tracked entries: {context.ChangeTracker.Entries().Count()}");

context.ChangeTracker.Clear();
var existing = context.Rows.Single(row => row.Id == 1);
existing.Name = "changed";
context.Rows.Update(existing);
Console.WriteLine("DbSet.Update modified properties: " + string.Join(",",
    context.Entry(existing).Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name)));
context.ChangeTracker.Clear();

// 用 DiagnosticListener 核验 DetachAll 是否触发全局 DetectChanges。
using var subscriptions = new DiagnosticSubscriptions(() => detections++);
_ = context.Rows.ToList();
var stateChanges = 0;
context.ChangeTracker.StateChanged += (_, _) => stateChanges++;
before = detections;
context.DetachAll();
Console.WriteLine($"DetachAll: DetectChanges={detections - before}, StateChanged={stateChanges}");
_ = context.Rows.ToList();
stateChanges = 0;
before = detections;
context.ChangeTracker.Clear();
Console.WriteLine($"Clear: DetectChanges={detections - before}, StateChanged={stateChanges}");

public sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
{
    public DbSet<ProbeRow> Rows => Set<ProbeRow>();
}

public sealed class ProbeRow : Entity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public Guid TenantId { get; set; }
}

public sealed class ProbeTenantCondition : GirvsRepositoryOtherQueryCondition
{
    public Expression<Func<ProbeRow, bool>> ForTenant(Guid tenantId) =>
        BuilderTenantBinaryExpression<ProbeRow>(tenantId);
}

public sealed class DiagnosticSubscriptions : IObserver<System.Diagnostics.DiagnosticListener>,
    IObserver<KeyValuePair<string, object>>, IDisposable
{
    private readonly Action _onDetection;
    private readonly List<IDisposable> _subscriptions = [];
    public DiagnosticSubscriptions(Action onDetection)
    {
        _onDetection = onDetection;
        _subscriptions.Add(System.Diagnostics.DiagnosticListener.AllListeners.Subscribe(this));
    }
    public void OnNext(System.Diagnostics.DiagnosticListener listener)
    {
        if (listener.Name == "Microsoft.EntityFrameworkCore") _subscriptions.Add(listener.Subscribe(this));
    }
    public void OnNext(KeyValuePair<string, object> value)
    {
        if (value.Key == CoreEventId.DetectChangesStarting.Name) _onDetection();
    }
    public void OnCompleted() { }
    public void OnError(Exception error) { }
    public void Dispose()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
    }
}
