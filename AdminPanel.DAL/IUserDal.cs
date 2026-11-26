using AdminPanel.DTO;

namespace AdminPanel.DAL.Interfaces;

// ЗАВДАННЯ iv (DI): BL залежить від інтерфейсу, а не від класу UserDal.
// Завдяки цьому в unit-тестах замість бази підставляється мок (Moq)
public interface IUserDal
{
    List<UserDto> GetAll();
    UserDto? GetById(int userId);
    UserDto? GetByLogin(string login);
    int Create(UserDto user);   // повертає Id нового користувача
    bool Update(UserDto user);  // true — рядок оновлено
}