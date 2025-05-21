sql_lib
-- Создаем таблицу авторов (основная таблица) CREATE TABLE authors ( author_id SERIAL PRIMARY KEY, first_name VARCHAR(100) NOT NULL, last_name VARCHAR(100) NOT NULL, biography TEXT, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP );

-- Создаем таблицу категорий (основная таблица) CREATE TABLE categories ( category_id SERIAL PRIMARY KEY, name VARCHAR(100) NOT NULL UNIQUE, description TEXT );

-- Создаем таблицу книг (основная таблица) CREATE TABLE books ( book_id SERIAL PRIMARY KEY, title VARCHAR(255) NOT NULL, isbn VARCHAR(20) UNIQUE NOT NULL, publication_date DATE, publisher VARCHAR(255), price DECIMAL(10, 2) NOT NULL, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP );

-- Создаем таблицу клиентов (основная таблица) CREATE TABLE customers ( customer_id SERIAL PRIMARY KEY, first_name VARCHAR(100) NOT NULL, last_name VARCHAR(100) NOT NULL, email VARCHAR(255) UNIQUE NOT NULL, phone VARCHAR(20), address TEXT, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP );

-- Создаем таблицу методов доставки (основная таблица) CREATE TABLE delivery_methods ( method_id SERIAL PRIMARY KEY, name VARCHAR(100) NOT NULL, description TEXT, cost DECIMAL(10, 2) NOT NULL );

-- Создаем таблицу заказов (связь "один-ко-многим" с клиентами и методами доставки) CREATE TABLE orders ( order_id SERIAL PRIMARY KEY, customer_id INT NOT NULL, order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP, status VARCHAR(50) NOT NULL, total_amount DECIMAL(10, 2) NOT NULL, delivery_method_id INT NOT NULL, FOREIGN KEY (customer_id) REFERENCES customers(customer_id) ON DELETE CASCADE, FOREIGN KEY (delivery_method_id) REFERENCES delivery_methods(method_id) );

-- Создаем таблицу экземпляров книг (связь "один-ко-многим" с книгами) CREATE TABLE book_copies ( copy_id SERIAL PRIMARY KEY, book_id INT NOT NULL, condition VARCHAR(50) NOT NULL, available BOOLEAN DEFAULT TRUE, FOREIGN KEY (book_id) REFERENCES books(book_id) ON DELETE CASCADE );

-- Создаем таблицу платежей (связь "один-ко-многим" с заказами) CREATE TABLE payments ( payment_id SERIAL PRIMARY KEY, order_id INT NOT NULL, amount DECIMAL(10, 2) NOT NULL, payment_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP, payment_method VARCHAR(50) NOT NULL, transaction_id VARCHAR(100), FOREIGN KEY (order_id) REFERENCES orders(order_id) ON DELETE CASCADE );

-- Создаем таблицу элементов заказа (связь "многие-ко-многим" между заказами и экземплярами книг) CREATE TABLE order_items ( order_id INT NOT NULL, copy_id INT NOT NULL, quantity INT NOT NULL DEFAULT 1, price_at_purchase DECIMAL(10, 2) NOT NULL, PRIMARY KEY (order_id, copy_id), FOREIGN KEY (order_id) REFERENCES orders(order_id) ON DELETE CASCADE, FOREIGN KEY (copy_id) REFERENCES book_copies(copy_id) );

-- Создаем таблицу связи книг и авторов (связь "многие-ко-многим") CREATE TABLE book_authors ( book_id INT NOT NULL, author_id INT NOT NULL, PRIMARY KEY (book_id, author_id), FOREIGN KEY (book_id) REFERENCES books(book_id) ON DELETE CASCADE, FOREIGN KEY (author_id) REFERENCES authors(author_id) ON DELETE CASCADE );

-- Создаем таблицу связи книг и категорий (связь "многие-ко-многим") CREATE TABLE book_categories ( book_id INT NOT NULL, category_id INT NOT NULL, PRIMARY KEY (book_id, category_id), FOREIGN KEY (book_id) REFERENCES books(book_id) ON DELETE CASCADE, FOREIGN KEY (category_id) REFERENCES categories(category_id) ON DELETE CASCADE );

Снимок экрана (241)

