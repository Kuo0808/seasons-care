using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Data;

namespace SeasonsCare.Api.Repositories
{
    // [架構導覽] 資料存取層 (Data Access Layer) - Repository
    // 職責：隔離對底層資料庫的直接依賴。封裝 O/RM (Entity Framework Core) 語法，與資料庫進行實際交涉。不應含有 if/else 的業務權限判斷。
    public class UserRepository : IUserRepository
    {
        private readonly DbContext _context;

        public UserRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Set<User>().AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Set<User>().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<User>> GetListByIdsAsync(IEnumerable<Guid> ids)
        {
            return await _context.Set<User>().Where(u => ids.Contains(u.Id)).ToListAsync();
        }

        public async Task AddAsync(User user)
        {
            // 單純宣告向 EF Core 的追蹤器 (Tracker) 註冊這筆新增，但不馬上異動資料庫實體
            await _context.Set<User>().AddAsync(user);
        }

        public Task UpdateAsync(User user)
        {
            _context.Set<User>().Update(user);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            // 將上述所有追蹤到的變更 (工作單元)，一次性送交指令給資料庫
            await _context.SaveChangesAsync();
        }
    }
}
