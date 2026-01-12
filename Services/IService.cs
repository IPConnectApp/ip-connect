public interface IService<T>
{
    Task<T> GetByIdAsync(int id);

    Task<IEnumerable<T>> GetAllAsync();

    Task<bool> CreateAsync(T entity);

    Task<bool> UpdateAsync(T entity);
    
    Task<bool> DeleteAsync(int id);
}