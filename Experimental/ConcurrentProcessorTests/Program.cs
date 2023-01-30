// See https://aka.ms/new-console-template for more information

using Kifa;
using Kifa.Service;

var processor = new ConcurrentProcessor<KifaActionResult> {
    Validator = KifaActionResult.ActionValidator,
    TotalRetryCount = 5,
    CooldownDuration = TimeSpan.FromSeconds(2)
};

processor.Start(8);

Console.WriteLine($"{DateTimeOffset.Now}: Started.");

var random = new Random();

for (var i = 0; i < 20; i++) {
    var i1 = i;
    processor.Add(() => {
        var v = random.Next(10);
        var status = v == i1 % 10 ? KifaActionResult.Success :
            v > 4 ? new KifaActionResult {
                Status = KifaActionStatus.Pending
            } : KifaActionResult.UnknownError;
        Console.WriteLine($"{DateTimeOffset.Now}: Run {i1}: {v} {status.Status}");

        return status;
    });
}

Console.WriteLine($"{DateTimeOffset.Now}: All added.");

processor.Stop();

Console.WriteLine($"{DateTimeOffset.Now}: All done.");
