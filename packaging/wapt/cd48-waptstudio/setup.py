# -*- coding: utf-8 -*-

from setuphelpers import *

import os
import shutil
import subprocess

package = '__PACKAGE_ID__'
version = '__PACKAGE_VERSION__'
section = 'base'
priority = 'optional'
name = 'WaptStudio'
categories = ['Utilities']
maturity = 'PROD'
target_os = 'windows'
architecture = 'x64'
maintainer = 'CD48'
description = 'Installe WaptStudio a partir du publish win-x64 self-contained.'
depends = []
conflicts = []
audit_schedule = '1d'

APP_DIRECTORY_NAME = 'WaptStudio'
APP_EXECUTABLE_NAME = 'WaptStudio.App.exe'
APP_SHORTCUT_NAME = 'WaptStudio.lnk'
CREATE_DESKTOP_SHORTCUT = False

PACKAGE_ROOT = os.path.dirname(os.path.abspath(__file__))
PUBLISHED_PAYLOAD_DIR = os.path.join(PACKAGE_ROOT, 'sources', 'app')
PROGRAM_FILES_ROOT = os.environ.get('ProgramW6432') or os.environ.get('ProgramFiles')
COMMON_START_MENU_DIR = os.path.join(os.environ.get('ProgramData', r'C:\ProgramData'), 'Microsoft', 'Windows', 'Start Menu', 'Programs')
PUBLIC_DESKTOP_DIR = os.path.join(os.environ.get('PUBLIC', r'C:\Users\Public'), 'Desktop')
INSTALL_ROOT = os.path.join(PROGRAM_FILES_ROOT, APP_DIRECTORY_NAME)
INSTALLED_EXECUTABLE = os.path.join(INSTALL_ROOT, APP_EXECUTABLE_NAME)
START_MENU_SHORTCUT = os.path.join(COMMON_START_MENU_DIR, APP_SHORTCUT_NAME)
DESKTOP_SHORTCUT = os.path.join(PUBLIC_DESKTOP_DIR, APP_SHORTCUT_NAME)


def _run_powershell(command):
    subprocess.check_call([
        'powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command',
        command,
    ])


def _stop_running_processes():
    subprocess.run(
        ['taskkill', '/IM', APP_EXECUTABLE_NAME, '/F'],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        check=False,
    )


def _remove_path(path):
    if os.path.isdir(path):
        shutil.rmtree(path)
    elif os.path.isfile(path):
        os.remove(path)


def _ensure_directory(path):
    os.makedirs(path, exist_ok=True)


def _create_shortcut(shortcut_path, target_path):
    escaped_shortcut = shortcut_path.replace("'", "''")
    escaped_target = target_path.replace("'", "''")
    escaped_workdir = INSTALL_ROOT.replace("'", "''")
    escaped_icon = target_path.replace("'", "''")

    command = (
        "$shell = New-Object -ComObject WScript.Shell; "
        "$shortcut = $shell.CreateShortcut('{0}'); "
        "$shortcut.TargetPath = '{1}'; "
        "$shortcut.WorkingDirectory = '{2}'; "
        "$shortcut.IconLocation = '{3},0'; "
        "$shortcut.Description = 'WaptStudio'; "
        "$shortcut.Save()"
    ).format(escaped_shortcut, escaped_target, escaped_workdir, escaped_icon)

    _run_powershell(command)


def _remove_shortcut(shortcut_path):
    if os.path.isfile(shortcut_path):
        os.remove(shortcut_path)


def _copy_payload():
    if not os.path.isdir(PUBLISHED_PAYLOAD_DIR):
        raise Exception('Payload publie introuvable: {0}'.format(PUBLISHED_PAYLOAD_DIR))

    _ensure_directory(PROGRAM_FILES_ROOT)
    if os.path.isdir(INSTALL_ROOT):
        shutil.rmtree(INSTALL_ROOT)

    shutil.copytree(PUBLISHED_PAYLOAD_DIR, INSTALL_ROOT)


def install():
    print('Installation de WaptStudio {0}'.format(version))
    _stop_running_processes()
    _copy_payload()
    _ensure_directory(COMMON_START_MENU_DIR)
    _create_shortcut(START_MENU_SHORTCUT, INSTALLED_EXECUTABLE)

    if CREATE_DESKTOP_SHORTCUT:
        _ensure_directory(PUBLIC_DESKTOP_DIR)
        _create_shortcut(DESKTOP_SHORTCUT, INSTALLED_EXECUTABLE)

    print('WaptStudio installe dans {0}'.format(INSTALL_ROOT))
    print('Les donnees utilisateur restent dans %LOCALAPPDATA%\\WaptStudio.')


def uninstall():
    print('Desinstallation de WaptStudio {0}'.format(version))
    _stop_running_processes()
    _remove_shortcut(START_MENU_SHORTCUT)

    if CREATE_DESKTOP_SHORTCUT:
        _remove_shortcut(DESKTOP_SHORTCUT)

    if os.path.isdir(INSTALL_ROOT):
        shutil.rmtree(INSTALL_ROOT)

    print('Les donnees utilisateur locales ne sont pas supprimees.')


def audit():
    if not os.path.isfile(INSTALLED_EXECUTABLE):
        raise Exception('Executable WaptStudio absent apres installation: {0}'.format(INSTALLED_EXECUTABLE))

    if not os.path.isfile(START_MENU_SHORTCUT):
        raise Exception('Raccourci Menu Demarrer absent: {0}'.format(START_MENU_SHORTCUT))

    print('Audit WaptStudio: installation coherente.')