namespace ip_connect.Repositories
{
    public interface IRepository<T> where T : class
    {
        // Create
        Task<T> CreateAsync(T entity);
        
        // Read
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        
        // Update
        Task<T> UpdateAsync(T entity);
        
        // Delete
        Task<bool> DeleteAsync(int id);
    }
}