import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QuoteEdit } from './quote-edit';

describe('QuoteEdit', () => {
  let component: QuoteEdit;
  let fixture: ComponentFixture<QuoteEdit>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuoteEdit],
    }).compileComponents();

    fixture = TestBed.createComponent(QuoteEdit);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
