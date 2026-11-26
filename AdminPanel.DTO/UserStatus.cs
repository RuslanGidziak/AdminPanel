namespace AdminPanel.DTO;

// DTO — прості класи для передачі даних між шарами (DAL → BL → ViewModels)
// Стан облікового запису. Числа збігаються зі стовпцем Status у таблиці tblUsers
public enum UserStatus : byte
{
    Pending = 0, // очікує активації (щойно зареєструвався)
    Active = 1,  // активний — може входити
    Blocked = 2  // заблокований адміністратором
}