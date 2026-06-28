import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService, EventItem, OccupancyReport, Reservation, Venue } from './api.service';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatSnackBarModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  activeTab = signal<'create' | 'events' | 'reservations'>('create');
  venues = signal<Venue[]>([]);
  events = signal<EventItem[]>([]);
  selectedEvent = signal<EventItem | null>(null);
  lastReservation = signal<Reservation | null>(null);
  report = signal<OccupancyReport | null>(null);
  loading = signal(false);

  readonly filtersForm = this.fb.nonNullable.group({
    type: [''],
    venueId: [''],
    status: [''],
    search: [''],
    from: [''],
    to: ['']
  });

  readonly eventForm = this.fb.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(5), Validators.maxLength(100)]),
    description: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]),
    venueId: this.fb.nonNullable.control(1, [Validators.required, Validators.min(1)]),
    maxCapacity: this.fb.nonNullable.control(20, [Validators.required, Validators.min(1)]),
    startDate: this.fb.control<Date | null>(null, Validators.required),
    startTime: this.fb.nonNullable.control('18:00', Validators.required),
    endDate: this.fb.control<Date | null>(null, Validators.required),
    endTime: this.fb.nonNullable.control('20:00', Validators.required),
    price: this.fb.nonNullable.control(50, [Validators.required, Validators.min(1)]),
    type: this.fb.nonNullable.control('conferencia', Validators.required)
  });

  readonly reservationForm = this.fb.nonNullable.group({
    quantity: [1, [Validators.required, Validators.min(1)]],
    buyerName: ['', Validators.required],
    buyerEmail: ['', [Validators.required, Validators.email]]
  });

  ngOnInit(): void {
    this.loadVenues();
    this.loadEvents();
  }

  loadVenues(): void {
    this.api.getVenues().subscribe({
      next: (venues) => this.venues.set(venues),
      error: (error) => this.setError(error)
    });
  }

  loadEvents(): void {
    this.loading.set(true);
    this.api.getEvents(this.filtersForm.getRawValue()).subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.setError(error);
      }
    });
  }

  createEvent(): void {
    if (this.eventForm.invalid) {
      this.eventForm.markAllAsTouched();
      this.notify('Completa los datos requeridos del evento.', 'warning');
      return;
    }

    const rawEvent = this.eventForm.getRawValue();
    const startAt = this.combineDateAndTime(rawEvent.startDate, rawEvent.startTime);
    const endAt = this.combineDateAndTime(rawEvent.endDate, rawEvent.endTime);

    if (!startAt || !endAt) {
      this.eventForm.markAllAsTouched();
      this.notify('Selecciona fecha y hora de inicio y fin.', 'warning');
      return;
    }

    this.api.createEvent({
      title: rawEvent.title,
      description: rawEvent.description,
      venueId: rawEvent.venueId,
      maxCapacity: rawEvent.maxCapacity,
      startAt,
      endAt,
      price: rawEvent.price,
      type: rawEvent.type
    }).subscribe({
      next: (eventItem) => {
        this.notify(`Evento creado: ${eventItem.title}`, 'success');
        this.activeTab.set('events');
        this.eventForm.reset({
          title: '',
          description: '',
          venueId: 1,
          maxCapacity: 20,
          startDate: null,
          startTime: '18:00',
          endDate: null,
          endTime: '20:00',
          price: 50,
          type: 'conferencia'
        });
        this.loadEvents();
      },
      error: (error) => this.setError(error)
    });
  }

  selectEvent(eventItem: EventItem): void {
    this.selectedEvent.set(eventItem);
    this.lastReservation.set(null);
    this.report.set(null);
    this.activeTab.set('reservations');
  }

  setActiveTab(tab: 'create' | 'events' | 'reservations'): void {
    this.activeTab.set(tab);
  }

  reserve(): void {
    const eventItem = this.selectedEvent();
    if (!eventItem || this.reservationForm.invalid) {
      this.reservationForm.markAllAsTouched();
      this.notify('Completa los datos de la reserva.', 'warning');
      return;
    }

    this.api.createReservation({
      eventId: eventItem.id,
      ...this.reservationForm.getRawValue()
    }).subscribe({
      next: (reservation) => {
        this.lastReservation.set(reservation);
        this.notify(`Reserva pendiente creada #${reservation.id}`, 'success');
      },
      error: (error) => this.setError(error)
    });
  }

  confirmPayment(): void {
    const reservation = this.lastReservation();
    if (!reservation) {
      return;
    }

    this.api.confirmPayment(reservation.id).subscribe({
      next: (updated) => {
        this.lastReservation.set(updated);
        this.notify(`Pago confirmado con codigo ${updated.reservationCode}`, 'success');
      },
      error: (error) => this.setError(error)
    });
  }

  cancelReservation(): void {
    const reservation = this.lastReservation();
    if (!reservation) {
      return;
    }

    this.api.cancelReservation(reservation.id).subscribe({
      next: (updated) => {
        this.lastReservation.set(updated);
        this.notify(`Reserva cancelada. Entradas perdidas: ${updated.lostTickets}`, 'warning');
      },
      error: (error) => this.setError(error)
    });
  }

  loadReport(): void {
    const eventItem = this.selectedEvent();
    if (!eventItem) {
      return;
    }

    this.api.getReport(eventItem.id).subscribe({
      next: (report) => this.report.set(report),
      error: (error) => this.setError(error)
    });
  }

  venueName(id: number): string {
    return this.venues().find((venue) => venue.id === id)?.name ?? `Venue ${id}`;
  }

  private setError(error: { error?: { detail?: string; title?: string } }): void {
    this.notify(error.error?.detail ?? error.error?.title ?? 'Ocurrio un error inesperado.', 'error');
  }

  private notify(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info'): void {
    this.snackBar.open(message, 'Cerrar', {
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['app-snackbar', `app-snackbar-${type}`]
    });
  }

  private combineDateAndTime(date: Date | null, time: string): string | null {
    const [hours, minutes] = time.split(':').map(Number);

    if (!date || Number.isNaN(hours) || Number.isNaN(minutes)) {
      return null;
    }

    const value = new Date(date);
    value.setHours(hours, minutes, 0, 0);

    return value.toISOString();
  }
}
