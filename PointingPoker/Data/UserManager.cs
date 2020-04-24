using Blazored.LocalStorage;
using Microsoft.JSInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class UserManager
    {
        const string USER_ID_KEY = "userId";
        private readonly ILocalStorageService _localStorage;

        public UserManager(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<string> GetUserIdAsync()
        {
            var userId = await _localStorage.GetItemAsync<string>(USER_ID_KEY);
            if (string.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString();
                await _localStorage.SetItemAsync(USER_ID_KEY, userId);
            }

            return userId;
        }
    }
}
