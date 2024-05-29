//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Core.Framework.Context
{
    public class RadiusContextFlags
    {
        /// <summary>
        /// Indicates that the radius response response will not be generated.
        /// </summary>
        public bool SkipResponseFlag { get; private set; }

        /// <summary>
        /// Indicates that radius pipeline is in terminal stage, that is, no user defined middleware will be executed anymore.
        /// </summary>
        public bool TerminateFlag { get; private set; }

        /// <summary>
        /// Do not generate radius response.
        /// </summary>
        public void SkipResponse() => SkipResponseFlag = true;

        /// <summary>
        /// Immediately terminates the pipeline execution and skips all the remaining user difined middlewares.
        /// </summary>
        public void Terminate() => TerminateFlag = true;
    }
}
