using HeadendHQ.Core.Assets.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Assets.CommandHandlers;

public record GetWordMarksQuery : ICommand<List<WordMark>>;

public class GetWordMarksHandler(IReadModel readModel)
    : ICommandHandler<GetWordMarksQuery, List<WordMark>>
{
    public async ValueTask<List<WordMark>> Handle(GetWordMarksQuery query, CancellationToken ct)
    {
        var results = await readModel.All<WordMark>(ct);
        return [.. results];
    }
}

public record CreateWordMarkCommand(League League, string Variant) : ICommand<WordMark>;

public class CreateWordMarkHandler(IWorkspace workspace)
    : ICommandHandler<CreateWordMarkCommand, WordMark>
{
    public async ValueTask<WordMark> Handle(CreateWordMarkCommand command, CancellationToken ct)
    {
        var existing = await workspace.LoadSingleOrDefault(
            new WordMarkByLeagueVariantSpec(command.League, command.Variant), ct);

        if (existing is not null)
            return existing;

        var wordMark = new WordMark(command.League, command.Variant);
        workspace.Add(wordMark);
        return wordMark;
    }
}

public record UploadWordMarkLogoCommand(int Id, byte[] LogoData) : ICommand<WordMark>;

public class UploadWordMarkLogoHandler(IWorkspace workspace, ILogoNormalizer normalizer)
    : ICommandHandler<UploadWordMarkLogoCommand, WordMark>
{
    public async ValueTask<WordMark> Handle(UploadWordMarkLogoCommand command, CancellationToken ct)
    {
        var wordMark = await workspace.LoadById<WordMark, int>(command.Id, ct);
        wordMark.LogoData = await normalizer.NormalizeWordMarkAsync(command.LogoData, ct);
        return wordMark;
    }
}

public record DeleteWordMarkCommand(int Id) : ICommand<Unit>;

public class DeleteWordMarkHandler(IWorkspace workspace)
    : ICommandHandler<DeleteWordMarkCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteWordMarkCommand command, CancellationToken ct)
    {
        var wordMark = await workspace.LoadById<WordMark, int>(command.Id, ct);
        workspace.Remove(wordMark);
        return Unit.Value;
    }
}
