namespace SnowyLib.Extensions.Extensions
{
    public static class EnemyAIExtensions
    {
        public static StatusEffectController StatusEffectController(this EnemyAI enemy)
        {
            return enemy.gameObject.TryGetComponent(out StatusEffectController controller) ? controller : enemy.gameObject.AddComponent<StatusEffectController>();
        }
    }
}
