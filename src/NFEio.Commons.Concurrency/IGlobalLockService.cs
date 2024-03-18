
namespace NFEio.Commons.Concurrency;
public interface IGlobalLockService
{
    IGlobalLock AcquireLock(string name, TimeSpan lockTime);

    Task<bool> DeleteLock(string name);
}
public interface IGlobalLock : IDisposable
{

}
