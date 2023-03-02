namespace SemanticBackup.Core.Interfaces
{
    public interface IProcessorInitializable
    {
        /// <summary>
        /// Initialized Service Functions. Ensure you are handling errors as well
        /// </summary>
        void Initialize();
    }
}
