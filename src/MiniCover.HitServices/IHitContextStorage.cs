namespace MiniCover.HitServices
{
    public interface IHitContextStorage
    {
        void Save(HitContext hitContext, string hitsPath);
        bool Clear(string hitsPath);
    }
}
