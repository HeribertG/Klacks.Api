// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.ML.OnnxRuntime;
using Tokenizers.DotNet;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Everything one ONNX inference run needs, copied out of the holder while its init lock is held.
/// A run works off this value and never reads a field again, which is what makes an unload
/// mid-inference impossible.
/// </summary>
/// <param name="Session">The native inference session; disposing it frees the model weights.</param>
/// <param name="Tokenizer">The tokenizer matching the session's vocabulary.</param>
/// <param name="OutputNames">Output names of the graph, needed for the RunOptions overload of Run.</param>
/// <param name="RunOptions">Per-run arena shrinkage when the profile asked for it; null otherwise.</param>
internal readonly record struct OnnxSessionLease(
    InferenceSession Session,
    Tokenizer Tokenizer,
    string[] OutputNames,
    RunOptions? RunOptions) : IDisposable
{
    public void Dispose()
    {
        RunOptions?.Dispose();
        Session.Dispose();
        Tokenizer.Dispose();
    }
}
