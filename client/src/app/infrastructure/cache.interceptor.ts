import { Injectable } from "@angular/core";
import { HttpInterceptor, HttpEvent, HttpHandler, HttpRequest, HttpResponse } from "@angular/common/http";
import { Observable } from "rxjs/Observable";
import { map, filter, tap } from 'rxjs/operators';
import 'rxjs/add/observable/of';

@Injectable()
export class CacheInterceptor implements HttpInterceptor {
    private cache: { [name: string]: HttpEvent<any> } = {};
    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (request.method !== "GET") {
            return next.handle(request);
        }
        const cachedResponse = this.cache[request.urlWithParams] || null;
        if (cachedResponse) {
            return Observable.of(cachedResponse);
        }
        return next.handle(request).pipe(tap(event => {
            if (event instanceof HttpResponse) {
                this.cache[request.urlWithParams] = event;
            }
            return event;
        }));
    }
}