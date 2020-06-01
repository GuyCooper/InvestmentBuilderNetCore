from app import app
from flask import render_template
from flask import request
from flask_login import current_user, login_user
from flask import render_template, flash, redirect, url_for
from flask_login import logout_user
from flask_login import login_required
from flask import jsonify
from app.models import *
from app.forms import LoginForm
from app.forms import RegistrationForm
import uuid

@app.route('/')
@app.route('/index')
@login_required
def index():
    return render_template('index.html', title='Home', user=current_user)

@app.route('/login', methods=['GET', 'POST'])
def login():
    if current_user.is_authenticated:
        return redirect(url_for('index'))
    form = LoginForm()
    if form.validate_on_submit():
        user = User(form.username, uuid.uuid4())
        user.is_authenticated = True
        user.is_active = True
        login_user(user, remember=form.remember_me.data)
        addUser(user)
        return redirect(url_for('index'))
    return render_template('login.html', title='Sign In', form=form)

@app.route('/logout')
def logout():
    logout_user()
    removeUser(current_user.id)
    return redirect(url_for('index'))

@app.route('/register', methods=['GET', 'POST'])
def register():
    if current_user.is_authenticated:
        return redirect(url_for('index'))
    form = RegistrationForm()
    if form.validate_on_submit():
        flash('Congratulations, you are now a registered user!')
        return redirect(url_for('login'))
    return render_template('register.html', title='Register', form=form)

@app.route('/command', methods=['GET', 'POST'])
@login_required
def command():
    print('command request received')
    for i in request.json:
        print('data ' + i)

    print('command: ' + request.json["command"])

    #name = request.form['command']
    #payload = request.form['payload']
    #return jsonify(data='you requested ' + name + ' and the payload was ' + payload)
    return jsonify(result='userid is ' + str(current_user.id))

