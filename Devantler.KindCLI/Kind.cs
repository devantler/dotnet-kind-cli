using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Exceptions;
using Devantler.CLIRunner;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Devantler.KindCLI;

/// <summary>
/// A class to run Kind CLI commands.
/// </summary>
public static class Kind
{
  /// <summary>
  /// The Kind CLI command.
  /// </summary>
  static Command Command => GetCommand();

  internal static Command GetCommand(PlatformID? platformID = default, Architecture? architecture = default, string? runtimeIdentifier = default)
  {
    platformID ??= Environment.OSVersion.Platform;
    architecture ??= RuntimeInformation.ProcessArchitecture;
    runtimeIdentifier ??= RuntimeInformation.RuntimeIdentifier;

    string binary = (platformID, architecture, runtimeIdentifier) switch
    {
      (PlatformID.Unix, Architecture.X64, "osx-x64") => "kind-osx-x64",
      (PlatformID.Unix, Architecture.Arm64, "osx-arm64") => "kind-osx-arm64",
      (PlatformID.Unix, Architecture.X64, "linux-x64") => "kind-linux-x64",
      (PlatformID.Unix, Architecture.Arm64, "linux-arm64") => "kind-linux-arm64",
      (PlatformID.Win32NT, Architecture.X64, "win-x64") => "kind-win-x64.exe",
      _ => throw new PlatformNotSupportedException($"Unsupported platform: {Environment.OSVersion.Platform} {RuntimeInformation.ProcessArchitecture}"),
    };
    string binaryPath = Path.Combine(AppContext.BaseDirectory, binary);
    return !File.Exists(binaryPath) ?
      throw new FileNotFoundException($"{binaryPath} not found.") :
      Cli.Wrap(binaryPath);
  }

  /// <summary>
  /// Creates a new Kind cluster.
  /// </summary>
  /// <param name="clusterName"></param>
  /// <param name="configPath"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task CreateClusterAsync(string clusterName, string configPath, CancellationToken cancellationToken)
  {
    var cmd = Command.WithArguments(
        [
          "create",
          "cluster",
          $"--name={clusterName}",
          $"--config={configPath}"
        ]
      );
    try
    {
      var (exitCode, _) = await CLI.RunAsync(cmd, cancellationToken: cancellationToken).ConfigureAwait(false);
      if (exitCode != 0)
      {
        throw new KindException($"Failed to create Kind cluster. Exit code: {exitCode}");
      }
    }
    catch (CommandExecutionException ex)
    {
      throw new KindException("Failed to create Kind cluster.", ex);
    }
  }

  /// <summary>
  /// Starts a Kind cluster.
  /// </summary>
  /// <param name="clusterName"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task StartClusterAsync(string clusterName, CancellationToken cancellationToken)
  {
    // docker start container
    using var config = new DockerClientConfiguration();
    var client = config.CreateClient();
    _ = await client.Containers
      .StartContainerAsync($"{clusterName}-control-plane", new ContainerStartParameters(), cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Stops a Kind cluster.
  /// </summary>
  /// <param name="clusterName"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task StopClusterAsync(string clusterName, CancellationToken cancellationToken)
  {
    // docker stop container
    using var config = new DockerClientConfiguration();
    var client = config.CreateClient();
    _ = await client.Containers
      .StopContainerAsync($"{clusterName}-control-plane", new ContainerStopParameters(), cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Deletes a Kind cluster.
  /// </summary>
  /// <param name="clusterName"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task DeleteClusterAsync(string clusterName, CancellationToken cancellationToken)
  {
    var cmd = Command.WithArguments($"delete cluster --name {clusterName}");
    try
    {
      var (exitCode, _) = await CLI.RunAsync(cmd, cancellationToken: cancellationToken).ConfigureAwait(false);
      if (exitCode != 0)
      {
        throw new KindException($"Failed to delete Kind cluster. Exit code: {exitCode}");
      }
    }
    catch (CommandExecutionException ex)
    {
      throw new KindException("Failed to delete Kind cluster.", ex);
    }
  }

  /// <summary>
  /// Gets a Kind cluster.
  /// </summary>
  /// <param name="clusterName"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task<bool> GetClusterAsync(string clusterName, CancellationToken cancellationToken)
  {
    string[] clusterNames = await ListClustersAsync(cancellationToken).ConfigureAwait(false);
    return clusterNames.Contains(clusterName);
  }

  /// <summary>
  /// Lists all Kind clusters.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async Task<string[]> ListClustersAsync(CancellationToken cancellationToken)
  {
    var cmd = Command.WithArguments("get clusters");
    try
    {
      var (_, result) = await CLI.RunAsync(cmd, cancellationToken: cancellationToken).ConfigureAwait(false);
      string[] clusterNames = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
      return clusterNames;
    }
    catch (CommandExecutionException ex)
    {
      throw new KindException("Failed to list Kind clusters.", ex);
    }
  }
}