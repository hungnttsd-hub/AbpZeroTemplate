export interface StorageAdapter {
  get<T>(key: string): Promise<T | undefined>;
  set<T>(key: string, value: T): Promise<void>;
  remove(key: string): Promise<void>;
  entries<T>(prefix: string): Promise<Array<{ key: string; value: T }>>;
}
class IndexedStorage implements StorageAdapter {
  private connection?: Promise<IDBDatabase>;
  private open() {
    return this.connection ??= new Promise<IDBDatabase>((resolve, reject) => {
      const request = indexedDB.open('wordy-wings', 1);
      request.onupgradeneeded = () => request.result.createObjectStore('records');
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => { this.connection = undefined; reject(new Error('Không thể mở bộ nhớ. Hãy cho phép lưu dữ liệu trên trình duyệt.')); };
      request.onblocked = () => reject(new Error('Hãy đóng tab Wordy Wings khác rồi thử lại.'));
    });
  }
  private async transaction<T>(mode: IDBTransactionMode, run: (store: IDBObjectStore) => IDBRequest<T>): Promise<T> {
    const db = await this.open();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('records', mode);
      const request = run(tx.objectStore('records'));
      tx.oncomplete = () => resolve(request.result);
      tx.onabort = tx.onerror = () => reject(tx.error ?? new Error('Bộ nhớ đầy. Hãy giải phóng dung lượng và thử lưu lại.'));
    });
  }
  get<T>(key: string) { return this.transaction<T | undefined>('readonly', store => store.get(key)); }
  async set<T>(key: string, value: T) { await this.transaction('readwrite', store => store.put(value, key)); }
  async remove(key: string) { await this.transaction('readwrite', store => store.delete(key)); }
  async entries<T>(prefix: string): Promise<Array<{ key: string; value: T }>> {
    const db = await this.open();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('records', 'readonly');
      const result: Array<{ key: string; value: T }> = [];
      const request = tx.objectStore('records').openCursor(IDBKeyRange.bound(prefix, prefix + '\uffff'));
      request.onsuccess = () => { const cursor = request.result; if (cursor) { result.push({ key: String(cursor.key), value: cursor.value as T }); cursor.continue(); } };
      tx.oncomplete = () => resolve(result); tx.onerror = () => reject(tx.error);
    });
  }
}
export const storage: StorageAdapter = new IndexedStorage();
