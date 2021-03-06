namespace Spawner.Modules
{
    public class SpecialPathStarter : BaseModule
    {
        public override void Apply(Entity e, ModuleWorker worker)
        {
            if (worker.linkedSpawnPath != null)
            {
                e.TriggerBehaviour(worker.pathStartIdentifier, worker.linkedSpawnPath);
            }
            worker.ProcessNext(e);
        }
    }
}