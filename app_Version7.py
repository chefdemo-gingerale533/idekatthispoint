import sys
import sqlite3
import json
from flask import Flask, request, jsonify
from PyQt5.QtWidgets import (
    QApplication, QMainWindow, QVBoxLayout, QWidget, QPushButton, QTextEdit, QLabel, QTableWidget, QTableWidgetItem
)
from PyQt5.QtCore import QThread

# Flask Server
app = Flask(__name__)

@app.route('/api/licenses', methods=['POST'])
def add_license():
    data = request.json
    if not data:
        return jsonify({'error': 'Invalid data'}), 400
    
    roblox_username = data.get('RobloxUsername')
    discord_username = data.get('DiscordUsername')
    items = data.get('Items')
    total = data.get('Total')
    purchase_date = data.get('PurchaseDate')
    
    if not all([roblox_username, discord_username, items, total, purchase_date]):
        return jsonify({'error': 'Missing fields'}), 400

    # Insert data into the database
    conn = sqlite3.connect('licenses.db')
    cursor = conn.cursor()
    cursor.execute('''
        INSERT INTO Licenses (roblox_username, discord_username, items, total, purchase_date)
        VALUES (?, ?, ?, ?, ?)
    ''', (roblox_username, discord_username, json.dumps(items), total, purchase_date))
    conn.commit()
    conn.close()

    return jsonify({'message': 'License added successfully'}), 201

@app.route('/api/licenses', methods=['GET'])
def get_licenses():
    conn = sqlite3.connect('licenses.db')
    cursor = conn.cursor()
    cursor.execute('SELECT * FROM Licenses')
    licenses = cursor.fetchall()
    conn.close()
    
    return jsonify([
        {
            'id': row[0],
            'roblox_username': row[1],
            'discord_username': row[2],
            'items': json.loads(row[3]),
            'total': row[4],
            'purchase_date': row[5]
        } for row in licenses
    ])

# Initialize the database
def init_db():
    conn = sqlite3.connect('licenses.db')
    cursor = conn.cursor()
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS Licenses (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            roblox_username TEXT,
            discord_username TEXT,
            items TEXT,
            total REAL,
            purchase_date TEXT
        )
    ''')
    conn.commit()
    conn.close()

# Thread for running Flask server
class ServerThread(QThread):
    def run(self):
        init_db()
        app.run(host='0.0.0.0', port=5000, threaded=True)

# PyQt GUI
class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Licensing Server")
        self.setGeometry(100, 100, 800, 600)

        # Layout and widgets
        layout = QVBoxLayout()

        self.start_server_button = QPushButton("Start Server")
        self.start_server_button.clicked.connect(self.start_server)
        layout.addWidget(self.start_server_button)

        self.refresh_data_button = QPushButton("Refresh Data")
        self.refresh_data_button.clicked.connect(self.load_licenses)
        layout.addWidget(self.refresh_data_button)

        self.data_table = QTableWidget()
        self.data_table.setColumnCount(5)
        self.data_table.setHorizontalHeaderLabels(["ID", "Roblox Username", "Discord Username", "Items", "Total", "Purchase Date"])
        layout.addWidget(self.data_table)

        self.logs = QTextEdit()
        self.logs.setReadOnly(True)
        layout.addWidget(QLabel("Logs:"))
        layout.addWidget(self.logs)

        container = QWidget()
        container.setLayout(layout)
        self.setCentralWidget(container)

        self.server_thread = None

    def start_server(self):
        if not self.server_thread:
            self.server_thread = ServerThread()
            self.server_thread.start()
            self.logs.append("Server started on http://0.0.0.0:5000")

    def load_licenses(self):
        conn = sqlite3.connect('licenses.db')
        cursor = conn.cursor()
        cursor.execute('SELECT * FROM Licenses')
        licenses = cursor.fetchall()
        conn.close()

        self.data_table.setRowCount(len(licenses))
        for row_idx, row_data in enumerate(licenses):
            for col_idx, col_data in enumerate(row_data):
                if col_idx == 3:  # Items column
                    col_data = json.loads(col_data)
                self.data_table.setItem(row_idx, col_idx, QTableWidgetItem(str(col_data)))

if __name__ == "__main__":
    app_flask = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    sys.exit(app_flask.exec_())
