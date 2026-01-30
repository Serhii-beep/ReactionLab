import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";
import { Observable } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiUrl;

    protected get<T>(path: string, params?: HttpParams): Observable<T> {
        return this.http.get<T>(`${this.baseUrl}${path}`, { params });
    }

    protected post<T>(path: string, body: unknown): Observable<T> {
        return this.http.post<T>(`${this.baseUrl}${path}`, body);
    }

    protected put<T>(path: string, body: unknown): Observable<T> {
        return this.http.put<T>(`${this.baseUrl}${path}`, body);
    }

    protected delete<T>(path: string): Observable<T> {
        return this.http.delete<T>(`${this.baseUrl}${path}`);
    }
}