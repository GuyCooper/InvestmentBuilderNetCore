from app import login

class User:
    def __init__(self):
        self.is_authenticated = False
        self.is_active = False
        self.is_anonymous = True
        self.id = 1
        self.name = Null

    def __init__(self, name, id):
        self.is_authenticated = True
        self.is_active = True
        self.is_anonymous = False
        self.id = id
        self.name = name

    def get_id(self):
        return self.id

userCache = {}

def addUser(user):
    userCache[user.id] = user

def removeUser(id):
    userCache.pop(id)

@login.user_loader
def load_user(id):
    if id in userCache:
        return userCache[id]
