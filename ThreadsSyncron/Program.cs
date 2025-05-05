namespace ThreadsSyncron
{
    internal class Program
    {
        public class AccountHolder
        {
            public string FirstName { get; }
            public string LastName { get; }
            public int CreditRating { get; }
            public DateTime RegistrationDate { get; }

            public AccountHolder(string firstName, string lastName, int creditRating, DateTime registrationDate)
            {
                FirstName = firstName;
                LastName = lastName;
                CreditRating = creditRating;
                RegistrationDate = registrationDate;
            }
        }

        public class Account
        {
            private object _balanceLock = new object();  // Критическая секция для потокобезопасности

            public AccountHolder Owner { get; }
            public DateTime OpenDate { get; }
            public DateTime? CloseDate { get; private set; }
            public decimal Balance { get; private set; }
            public List<Operation> Transactions { get; } = new List<Operation>();

            public Account(AccountHolder owner, decimal initialBalance)
            {
                Owner = owner;
                OpenDate = DateTime.Now;
                Balance = initialBalance;
            }

            public void Deposit(decimal amount)
            {
                lock (_balanceLock)
                {
                    Balance += amount;
                    Transactions.Add(new Operation("Приход", amount));
                }
            }

            public bool Withdraw(decimal amount)
            {
                lock (_balanceLock)
                {
                    if (Balance >= amount)
                    {
                        Balance -= amount;
                        Transactions.Add(new Operation("Расход", amount));
                        return true;
                    }
                    return false;
                }
            }

            public void CloseAccount()
            {
                CloseDate = DateTime.Now;
            }

            public class Operation
            {
                public string Type { get; }
                public decimal Amount { get; }
                public DateTime TimeStamp { get; }
                public string Status { get; private set; }

                private static Mutex transactionMutex = new Mutex();  // Мьютекс для безопасной работы с переводами

                public Operation(string type, decimal amount)
                {
                    Type = type;
                    Amount = amount;
                    TimeStamp = DateTime.Now;
                    Status = "Выполняется";
                }

                public static void Transfer(Account from, Account to, decimal amount)
                {
                    new Thread(() =>
                    {
                        transactionMutex.WaitOne();  // Захват мьютекса

                        try
                        {
                            if (from.Withdraw(amount))
                            {
                                to.Deposit(amount);
                                Console.WriteLine($"Перевод {amount} грн выполнен.");
                            }
                            else
                            {
                                Console.WriteLine($"Недостаточно средств, ожидание...");
                                Thread.Sleep(3000);  // Ожидание 3 сек пополнения
                                if (!from.Withdraw(amount))
                                {
                                    Console.WriteLine($"Перевод отменен.");
                                }
                            }
                        }
                        finally
                        {
                            transactionMutex.ReleaseMutex();  // Освобождение мьютекса
                        }
                    }).Start();
                }
                static void Main(string[] args)
                {
                    AccountHolder person1 = new AccountHolder("Иван", "Петров", 800, DateTime.Now);
                    AccountHolder person2 = new AccountHolder("Анна", "Сидорова", 900, DateTime.Now);

                    Account accountA = new Account(person1, 5000);
                    Account accountB = new Account(person2, 2000);

                    Thread thread1 = new Thread(() => Operation.Transfer(accountA, accountB, 3000));
                    Thread thread2 = new Thread(() => Operation.Transfer(accountB, accountA, 4000));

                    thread1.Start();
                    thread2.Start();

                    thread1.Join();
                    thread2.Join();

                    Console.WriteLine($"Баланс счета A: {accountA.Balance}");
                    Console.WriteLine($"Баланс счета B: {accountB.Balance}");
                }

            }
        }
    }
}
