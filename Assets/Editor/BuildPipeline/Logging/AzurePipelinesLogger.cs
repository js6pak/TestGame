// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Buildalon.Editor.BuildPipeline.Logging
{
    /// <summary>
    /// Azure Pipelines CI Logger
    /// https://docs.microsoft.com/en-us/azure/devops/pipelines/scripts/logging-commands
    /// </summary>
    public class AzurePipelinesLogger : AbstractCILogger
    {
        /// <inheritdoc />
        public override string Error
        {
            get { return "##vso[task.logissue type=error;]"; }
        }

        /// <inheritdoc />
        public override string Warning
        {
            get { return "##vso[task.logissue type=warning;]"; }
        }
    }
}
